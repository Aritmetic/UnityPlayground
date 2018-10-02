using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NWH.VehiclePhysics
{
    public class Settings : MonoBehaviour
    {
        public VehicleChanger vehicleChanger;
        private VehicleController vc;
        public List<Setting> settingList = new List<Setting>();
        public GameObject settingPrefab;

        public float startXOffset = 230f;
        public float startYOffset = -60f;
        private float xOffset;
        private float yOffset;
        public float xStep = 400f;
        public float yStep = -16f;
        public float yLimit = -500f;

        public class Setting
        {
            public FieldInfo field;
            public object obj;
            public Text nameField;
            public Text valueField;
            public Button leftButton;
            public Button rightButton;
            public GameObject settingObject;
            public float min;
            public float max;
            public float step;
        }

        private void Redraw()
        {
            vc = vehicleChanger.ActiveVehicleController;
            if (vc == null)
                return;

            xOffset = startXOffset;
            yOffset = startYOffset;

            AddTitle("Engine");
            AddSettings(vc.engine);
            AddSettings(vc.engine.forcedInduction);

            AddTitle("Transmission");
            AddSettings(vc.transmission);

            AddTitle("Steering");
            AddSettings(vc.steering);

            AddTitle("Driving Assists");
            AddTitle("TCS", true);
            AddSettings(vc.drivingAssists.tcs);
            AddTitle("ABS", true);
            AddSettings(vc.drivingAssists.abs);
            AddTitle("Stability", true);
            AddSettings(vc.drivingAssists.stability);
            AddTitle("Drift Assist", true);
            AddSettings(vc.drivingAssists.driftAssist);

            // Display axles
            for (int i = 0; i < 2; i++)
            {
                NewColumn();
                Axle axle = vc.axles[i];
                if (axle == null) return;
                AddTitle("Axle " + i);
                AddSettings(axle);
                AddSettings(axle.geometry);

                AddTitle("Left Wheel, Axle " + i, true);
                AddSettings(axle.leftWheel.wheelController);
                AddSettings(axle.leftWheel.wheelController.wheel);
                AddSettings(axle.leftWheel.wheelController.spring);
                AddSettings(axle.leftWheel.wheelController.damper);
                AddSettings(axle.leftWheel.wheelController.fFriction);
                AddSettings(axle.leftWheel.wheelController.sFriction);

                AddTitle("Right Wheel, Axle " + i, true);
                AddSettings(axle.rightWheel.wheelController);
                AddSettings(axle.rightWheel.wheelController.wheel);
                AddSettings(axle.rightWheel.wheelController.spring);
                AddSettings(axle.rightWheel.wheelController.damper);
                AddSettings(axle.rightWheel.wheelController.fFriction);
                AddSettings(axle.rightWheel.wheelController.sFriction);
            }
        }

        public void Clear()
        {
            foreach(Setting setting in settingList)
            {
                Destroy(setting.settingObject);
            }
            settingList.Clear();
        }

        private void Start()
        {
            Redraw();
        }

        private void Update()
        {
            if(vehicleChanger.ActiveVehicleController != vc)
            {
                vc = vehicleChanger.ActiveVehicleController;
                Redraw();
            }

            if (vehicleChanger.ActiveVehicleController == null)
            {
                Clear();
                return;
            }

            foreach (Setting setting in settingList)
            {
                if(setting.valueField != null)
                {
                    setting.valueField.text = setting.field.GetValue(setting.obj).ToString();
                }
            }
        }

        private void AddSettings(object obj)
        {
            foreach (FieldInfo field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField |
                BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (!field.IsDefined(typeof(ShowInSettings), false)) continue;

                AddSetting(field, obj);

                if(yOffset < yLimit)
                {
                    NewColumn();
                }
            }
        }

        private void NewColumn()
        {
            xOffset += xStep;
            yOffset = startYOffset;
        }

        public void AddTitle(string text, bool subtitle = false)
        {
            GameObject title = new GameObject();
            title.name = text;
            Text titleText = title.AddComponent<Text>();
            titleText.font = Resources.Load<Font>("Fonts/DroidSansMono");
            titleText.text = text;
            titleText.fontSize = 12;
            if(!subtitle) titleText.fontStyle = FontStyle.Bold;
            title.transform.SetParent(this.gameObject.transform, false);
            RectTransform titleRT = title.GetComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(200, 15);
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(0, 1);
            if(!subtitle) yOffset -= 6f;
            titleRT.anchoredPosition = new Vector2(xOffset - 30, yOffset);
            yOffset += yStep;
            settingList.Add(new Setting() { settingObject = title });
        }

        public void AddSetting(FieldInfo field, object obj)
        {
            Setting setting = new Setting();
            setting.field = field;
            setting.obj = obj;
            setting.settingObject = Instantiate(settingPrefab) as GameObject;
            setting.settingObject.name = field.Name + "Setting";
            setting.settingObject.transform.SetParent(this.gameObject.transform, false);
            setting.settingObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(xOffset, yOffset);
            yOffset += yStep;

            setting.nameField = setting.settingObject.transform.GetChild(0).GetComponent<Text>();
            setting.valueField = setting.settingObject.transform.GetChild(1).GetComponent<Text>();
            setting.leftButton = setting.settingObject.transform.GetChild(2).GetComponent<Button>();
            setting.rightButton = setting.settingObject.transform.GetChild(3).GetComponent<Button>();

            setting.nameField.text = field.Name;


            if (field.FieldType == typeof(float))
            {
                setting.valueField.text = ((float)field.GetValue(obj)).ToString("0.00");

                var attribute = field.GetCustomAttributes(typeof(ShowInSettings), false).Cast<ShowInSettings>().FirstOrDefault();
                setting.min = attribute.min;
                setting.max = attribute.max;
                setting.step = attribute.step;
                if (attribute.name != null) setting.nameField.text = attribute.name;

                setting.leftButton.onClick.AddListener(() => { Decrement(setting); });
                setting.rightButton.onClick.AddListener(() => { Increment(setting); });
            }
            else if (field.FieldType == typeof(bool))
            {
                setting.valueField.text = field.GetValue(obj).ToString();
                setting.leftButton.onClick.AddListener(() => { ToggleBool(setting); });
                setting.rightButton.onClick.AddListener(() => { ToggleBool(setting); });
            }

            settingList.Add(setting);
        }

        public void ToggleBool(Setting setting)
        {
            bool currentValue = (bool)setting.field.GetValue(setting.obj);
            currentValue = !currentValue;
            setting.field.SetValue(setting.obj, currentValue);
        }

        public void Increment(Setting setting)
        {
            float currentValue = (float)setting.field.GetValue(setting.obj);
            currentValue = Mathf.Clamp(currentValue += setting.step, setting.min, setting.max);
            setting.field.SetValue(setting.obj, currentValue);
        }

        public void Decrement(Setting setting)
        {
            float currentValue = (float)setting.field.GetValue(setting.obj);
            currentValue = Mathf.Clamp(currentValue -= setting.step, setting.min, setting.max);
            setting.field.SetValue(setting.obj, currentValue);
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class ShowInSettings : System.Attribute
    {
        public string name = null;
        public float min;
        public float max;
        public float step;

        public ShowInSettings(float min, float max, float step)
        {
            this.min = min;
            this.max = max;
            this.step = step;
        }

        public ShowInSettings(string name, float min, float max, float step)
        {
            this.name = name;
            this.min = min;
            this.max = max;
            this.step = step;
        }

        public ShowInSettings() {}
    }
}

