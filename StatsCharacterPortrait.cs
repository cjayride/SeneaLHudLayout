using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Head-and-shoulders snapshot of the local character (hair, beard, helmet, chest, cape),
    /// captured with a hidden player clone the same way SocialSystem 2.0 portraits work.
    /// </summary>
    static class StatsCharacterPortrait
    {
        const int Layer = 31;
        const int Size = 128;
        const int SettleFrames = 2;
        const float CameraYaw = 205f;
        const float CameraDistance = 1.15f;
        const float CameraFieldOfView = 22f;
        static readonly Vector3 Stage = new Vector3(0f, -10000f, 0f);
        static readonly Vector3 HeadCenterOffset = new Vector3(0f, 0.08f, 0f);
        static readonly Color SunColor = new Color(1.15f, 1.02f, 0.85f);
        static readonly Vector3 SunDirection = new Vector3(-0.4f, -0.6f, -0.7f).normalized;
        static readonly Color AmbientColor = new Color(0.42f, 0.38f, 0.34f);
        static readonly Color Backdrop = new Color(0.12f, 0.09f, 0.06f, 1f);
        static readonly int WetId = Shader.PropertyToID("_Wet");

        static readonly FieldInfo SkinField = AccessTools.Field(typeof(Player), "m_skinColor");
        static readonly FieldInfo HairHash = AccessTools.Field(typeof(VisEquipment), "m_hairItem");
        static readonly FieldInfo BeardHash = AccessTools.Field(typeof(VisEquipment), "m_beardItem");
        static readonly FieldInfo HelmetHash = AccessTools.Field(typeof(VisEquipment), "m_helmetItem");
        static readonly FieldInfo ChestHash = AccessTools.Field(typeof(VisEquipment), "m_chestItem");
        static readonly FieldInfo ShoulderHash = AccessTools.Field(typeof(VisEquipment), "m_shoulderItem");
        static readonly FieldInfo ShoulderVariant = AccessTools.Field(typeof(VisEquipment), "m_shoulderItemVariant");
        static readonly FieldInfo ShoulderQuality = AccessTools.Field(typeof(VisEquipment), "m_shoulderItemQuality");

        static GameObject _rig;
        static PortraitRunner _runner;
        static GameObject _model;
        static VisEquipment _vis;
        static Animator _animator;
        static Transform _head;
        static Camera _camera;
        static Light _light;
        static RenderTexture _target;
        static Texture2D _cached;
        static Look _cachedLook;
        static Look _pendingLook;
        static bool _shooting;
        static int _dressedFrame;
        static readonly List<Action<Texture2D>> _callbacks = new List<Action<Texture2D>>();

        struct Look : IEquatable<Look>
        {
            public int Model;
            public Vector3 Skin;
            public Vector3 HairColor;
            public int Hair;
            public int Beard;
            public int Helmet;
            public int Chest;
            public int Shoulder;
            public int ShoulderVar;
            public int ShoulderQual;

            public bool Equals(Look other)
            {
                return Model == other.Model
                    && Hair == other.Hair
                    && Beard == other.Beard
                    && Helmet == other.Helmet
                    && Chest == other.Chest
                    && Shoulder == other.Shoulder
                    && ShoulderVar == other.ShoulderVar
                    && ShoulderQual == other.ShoulderQual
                    && Skin == other.Skin
                    && HairColor == other.HairColor;
            }
        }

        public static bool TryGet(Player player, out Texture2D texture)
        {
            texture = null;
            if (player == null || _cached == null)
            {
                return false;
            }

            if (!_cachedLook.Equals(ReadLook(player)))
            {
                return false;
            }

            texture = _cached;
            return true;
        }

        public static bool Request(Player player, Action<Texture2D> onReady)
        {
            if (player == null || ZNet.instance == null || ZNet.instance.IsDedicated())
            {
                return false;
            }

            Look look = ReadLook(player);
            if (_cached != null && _cachedLook.Equals(look))
            {
                onReady?.Invoke(_cached);
                return true;
            }

            if (onReady != null)
            {
                _callbacks.Add(onReady);
            }

            if (_shooting && _pendingLook.Equals(look))
            {
                return true;
            }

            if (!EnsureRig())
            {
                _callbacks.Clear();
                return false;
            }

            Dress(look);
            _runner.enabled = true;
            return true;
        }

        internal static void Step()
        {
            if (!_shooting)
            {
                if (_runner != null)
                {
                    _runner.enabled = false;
                }

                return;
            }

            if (Time.frameCount - _dressedFrame < SettleFrames)
            {
                if (_vis != null)
                {
                    _vis.CustomUpdate(0f, Time.time);
                }

                return;
            }

            Shoot();
        }

        static Look ReadLook(Player player)
        {
            VisEquipment vis = player.GetComponent<VisEquipment>();
            var look = new Look();
            look.Model = player.GetPlayerModel();
            if (SkinField != null)
            {
                object skin = SkinField.GetValue(player);
                if (skin is Vector3 skinVec)
                {
                    look.Skin = skinVec;
                }
            }

            look.HairColor = player.GetHairColor();
            if (vis == null)
            {
                return look;
            }

            look.Hair = IntField(HairHash, vis);
            look.Beard = IntField(BeardHash, vis);
            look.Helmet = IntField(HelmetHash, vis);
            look.Chest = IntField(ChestHash, vis);
            look.Shoulder = IntField(ShoulderHash, vis);
            look.ShoulderVar = IntField(ShoulderVariant, vis);
            look.ShoulderQual = IntField(ShoulderQuality, vis);
            return look;
        }

        static int IntField(FieldInfo field, object target)
        {
            if (field == null || target == null)
            {
                return 0;
            }

            object value = field.GetValue(target);
            return value is int i ? i : 0;
        }

        static void Dress(Look look)
        {
            _pendingLook = look;
            _shooting = true;
            _dressedFrame = Time.frameCount;
            _vis.SetModel(look.Model);
            _vis.SetSkinColor(look.Skin);
            _vis.SetHairColor(look.HairColor);
            _vis.SetHairItem(look.Hair);
            _vis.SetBeardItem(look.Beard);
            _vis.SetHelmetItem(look.Helmet);
            _vis.SetChestItem(look.Chest);
            _vis.SetShoulderItem(look.Shoulder, look.ShoulderVar, look.ShoulderQual);
            _model.SetActive(true);
            _animator.Play("Movement");
            _animator.Update(0f);
            _vis.CustomUpdate(0f, Time.time);
        }

        static void Shoot()
        {
            SetLayer(_model.transform);
            Quaternion rot = Quaternion.Euler(0f, CameraYaw, 0f);
            Vector3 focus = _head.position + HeadCenterOffset;
            _camera.transform.SetPositionAndRotation(focus - rot * Vector3.forward * CameraDistance, rot);
            Light sun = EnvMan.instance != null ? EnvMan.instance.m_dirLight : null;
            int sunMask = sun != null ? sun.cullingMask : 0;
            bool fog = RenderSettings.fog;
            AmbientMode ambientMode = RenderSettings.ambientMode;
            Color ambientLight = RenderSettings.ambientLight;
            float wet = Shader.GetGlobalFloat(WetId);
            try
            {
                _light.gameObject.SetActive(true);
                if (sun != null)
                {
                    sun.cullingMask &= 0x7FFFFFFF;
                }

                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = AmbientColor;
                Shader.SetGlobalFloat(WetId, 0f);
                _camera.Render();
            }
            finally
            {
                _light.gameObject.SetActive(false);
                if (sun != null)
                {
                    sun.cullingMask = sunMask;
                }

                RenderSettings.fog = fog;
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambientLight;
                Shader.SetGlobalFloat(WetId, wet);
                _model.SetActive(false);
            }

            RenderTexture previous = RenderTexture.active;
            var copy = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "SeneaL_CharPortrait"
            };
            RenderTexture.active = _target;
            copy.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0, false);
            copy.Apply(false, true);
            RenderTexture.active = previous;
            _shooting = false;
            if (_cached != null && _cached != copy)
            {
                UnityEngine.Object.Destroy(_cached);
            }

            _cached = copy;
            _cachedLook = _pendingLook;
            Action<Texture2D>[] ready = _callbacks.ToArray();
            _callbacks.Clear();
            if (_runner != null)
            {
                _runner.enabled = false;
            }

            for (int i = 0; i < ready.Length; i++)
            {
                try
                {
                    ready[i]?.Invoke(copy);
                }
                catch (Exception)
                {
                }
            }
        }

        static bool EnsureRig()
        {
            if (_rig != null)
            {
                return _vis != null && _camera != null && _head != null;
            }

            GameObject prefab = Game.instance != null ? Game.instance.m_playerPrefab : null;
            if (prefab == null)
            {
                return false;
            }

            _rig = new GameObject("SeneaLHudLayout_PortraitStudio");
            _rig.transform.position = Stage;
            bool forceDisable = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                _model = UnityEngine.Object.Instantiate(prefab, Stage, Quaternion.identity, _rig.transform);
            }
            finally
            {
                ZNetView.m_forceDisableInit = forceDisable;
            }

            Player clonePlayer = _model.GetComponent<Player>();
            if (clonePlayer != null)
            {
                Player.GetAllPlayers().Remove(clonePlayer);
                Character.GetAllCharacters().Remove(clonePlayer);
            }

            Rigidbody body = _model.GetComponent<Rigidbody>();
            if (body != null)
            {
                UnityEngine.Object.Destroy(body);
            }

            MonoBehaviour[] behaviours = _model.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is VisEquipment))
                {
                    behaviours[i].enabled = false;
                }
            }

            CharacterAnimEvent[] animEvents = _model.GetComponentsInChildren<CharacterAnimEvent>(true);
            for (int i = 0; i < animEvents.Length; i++)
            {
                animEvents[i].enabled = false;
            }

            _vis = _model.GetComponent<VisEquipment>();
            _animator = _model.GetComponentInChildren<Animator>();
            if (_vis == null || _animator == null)
            {
                UnityEngine.Object.Destroy(_rig);
                _rig = null;
                return false;
            }

            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            SkinnedMeshRenderer[] skins = _model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                skins[i].updateWhenOffscreen = true;
            }

            _head = Utils.GetBoneTransform(_animator, HumanBodyBones.Head);
            if (_head == null)
            {
                UnityEngine.Object.Destroy(_rig);
                _rig = null;
                return false;
            }

            SetLayer(_model.transform);
            _model.SetActive(false);
            _target = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32)
            {
                name = "SeneaL_PortraitTarget"
            };
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(_rig.transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.cullingMask = 1 << Layer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Backdrop;
            _camera.renderingPath = RenderingPath.Forward;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.fieldOfView = CameraFieldOfView;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 5f;
            _camera.targetTexture = _target;
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(_rig.transform, false);
            lightGo.transform.rotation = Quaternion.LookRotation(SunDirection);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.color = SunColor;
            _light.intensity = 1f;
            _light.shadows = LightShadows.None;
            _light.cullingMask = 1 << Layer;
            lightGo.SetActive(false);
            _runner = _rig.AddComponent<PortraitRunner>();
            _runner.enabled = false;
            return true;
        }

        static void SetLayer(Transform root)
        {
            root.gameObject.layer = Layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayer(root.GetChild(i));
            }
        }

        class PortraitRunner : MonoBehaviour
        {
            void LateUpdate()
            {
                Step();
            }
        }
    }
}
