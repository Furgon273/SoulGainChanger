using System;
using System.Collections;
using System.Collections.Generic;
using Modding;
using Modding.Converters;
using UnityEngine;
using InControl;
using Newtonsoft.Json;

namespace SoulGainChanger
{
    public class KeyBinds : PlayerActionSet
    {
        //the keybinds you want to save. it needs to be of type PlayerAction
        public PlayerAction IncSoulGain, DecSoulGain;
        //a constructor to initalize the PlayerAction
        public KeyBinds()
        {
            IncSoulGain = CreatePlayerAction("IncSoulGain");
            DecSoulGain = CreatePlayerAction("DecSoulGain");
            //optional: set a default bind
            IncSoulGain.AddDefaultBinding(Key.O);
            DecSoulGain.AddDefaultBinding(Key.P);
        }
    }
    [Serializable]
    public class GlobalSettings
    {
        public bool EnabledMod = true;
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds keybinds = new KeyBinds();
    }
    internal class ModDisplay
    {
        internal static ModDisplay Instance;
        private string DisplayText = "";
        private Vector2 TextSize = new(800, 500);
        private Vector2 TextPosition = new(0.78f, 0.243f);
        private GameObject _canvas;
        private UnityEngine.UI.Text _text;
        public ModDisplay()
        {
            Create();
        }
        private void Create()
        {
            if (_canvas != null) return;
            // Create base canvas
            _canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            CanvasGroup canvasGroup = _canvas.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            UnityEngine.Object.DontDestroyOnLoad(_canvas);
            _text = CanvasUtil.CreateTextPanel(
                _canvas, "Soul Gain: 6", 24, TextAnchor.LowerRight,
                new CanvasUtil.RectData(TextSize, Vector2.zero, TextPosition, TextPosition),
                CanvasUtil.GetFont("Perpetua")
            ).GetComponent<UnityEngine.UI.Text>();
            _canvas.SetActive(true);
        }
        public void Update()
        {
            _text.text = DisplayText;
        }
        public void Display(string text)
        {
            DisplayText = text.Trim();
            Update();
        }
    }
    public class SoulGainChanger : Mod, IMenuMod, IGlobalSettings<GlobalSettings>
    {
        public static GlobalSettings GS { get; private set; } = new();
        public void OnLoadGlobal(GlobalSettings s) => GS = s;
        public GlobalSettings OnSaveGlobal() => GS;
        public SoulGainChanger() : base("SoulGainChanger") { }
        public override string GetVersion() => "v2.1.0.0";
        public int SoulNow;
        public int SoulGain;
        public int SoulWhenFocus;
        public override void Initialize()
        {
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModDisplay.Instance = new ModDisplay();
            SoulNow = 0; SoulGain = 6;
        }
        bool IMenuMod.ToggleButtonInsideMenu => true;
        public bool ToggleButtonInsideMenu => true;
        List<IMenuMod.MenuEntry> IMenuMod.GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            return new List<IMenuMod.MenuEntry>
            {
                new IMenuMod.MenuEntry
                {
                    Name = "Global Switch",
                    Description = "Turn mod On/Off",
                    Values = new string[] {
                        "Off",
                        "On",
                    },
                    Saver = opt => ChangeGlobalSwitchState(opt == 1),
                    Loader = () => GS.EnabledMod ? 1 : 0
                }
            };
        }
        public void ChangeGlobalSwitchState(bool state)
        {
            GS.EnabledMod = state;
            if (state)
            {
                ModDisplay.Instance.Display("Soul Gain: " + Convert.ToString(SoulGain));
            }
            else
            {
                ModDisplay.Instance.Display("");
            }
        }
        public void OnHeroUpdate()
        {
            if (!GS.EnabledMod)
            {
                return;
            }
            if (PlayerData.instance.MPCharge == 0)
            {
                SoulNow = 0;
                PlayerData.instance.MPCharge = 1;
            }
            if (SoulNow == 33)
            {
                if ((PlayerData.instance.MPCharge > SoulWhenFocus) && (SoulWhenFocus != 33))
                {
                    PlayerData.instance.MPCharge -= (SoulWhenFocus - 1);
                    SoulNow = SoulWhenFocus;
                }
                SoulWhenFocus = PlayerData.instance.MPCharge;
            }
            if ((PlayerData.instance.MPCharge > 1) && (SoulNow < 33))
            {
                SoulNow += (PlayerData.instance.MPCharge - 1) / 11 * SoulGain;
                if (SoulNow >= 33)
                {
                    SoulNow = 33;
                    SoulWhenFocus = 33;
                }
                PlayerData.instance.MPCharge = 0;
                HeroController.instance.AddMPCharge(SoulNow);
                if (SoulNow != 33)
                {
                    PlayerData.instance.MPCharge = 1;
                }
            }
            if (PlayerData.instance.MPCharge > 33)
            {
                HeroController.instance.TakeMPQuick(PlayerData.instance.MPCharge - 33);
            }
            if (GS.keybinds.IncSoulGain.WasPressed)
            {
                SoulGain++;
                ModDisplay.Instance.Display("Soul Gain: " + Convert.ToString(SoulGain));
            }
            if (GS.keybinds.DecSoulGain.WasPressed)
            {
                SoulGain--;
                ModDisplay.Instance.Display("Soul Gain: " + Convert.ToString(SoulGain));
            }
        }
    }
}
