using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NWH.VehiclePhysics
{
    public class Telemetry : MonoBehaviour
    {
        public VehicleChanger vehicleChanger;
        public List<string> pages = new List<string>();
        public List<Text> columns = new List<Text>();
        public int linesPerColumn;

        private int lineCount;
        private string text;
        private VehicleController vc;

        void LateUpdate()
        {
            if (Time.frameCount % 5 != 0) return;

            vc = vehicleChanger.ActiveVehicleController;
            if (vc == null)
                return;

            // Build strings
            AddTitle("Vehicle", '_');
            PrintProperties(vc);
            AddLine("Odometer", vc.metrics.odometer.value);
            AddLine("TopSpeed", vc.metrics.topSpeed.value);
            AddLine("AverageSpeed", vc.metrics.averageSpeed.value);
            AddLine("ContinousDriftTime", vc.metrics.continousDriftTime.value);
            AddLine("FuelConsumptionMPG", vc.fuel.ConsumptionMPG);
            AddLine("FuelConsumptionl100km", vc.fuel.ConsumptionLitersPer100Kilometers);
            AddLine("FuelConsumptionKmPerLiter", vc.fuel.ConsumptionKilometersPerLiter);
            AddSpace();

            AddTitle("Engine", '_');
            PrintProperties(vc.engine);
            AddTitle("Forced Induction", ' ');
            PrintProperties(vc.engine.forcedInduction);
            AddSpace();

            AddTitle("Transmission", '_');
            PrintProperties(vc.transmission);
            AddSpace();

            SavePage();

            int count = 0;
            foreach(Axle axle in vc.axles)
            {
                AddTitle("Axle " + count, '_');
                PrintProperties(axle);
                AddTitle("Geometry", ' ');
                PrintProperties(axle.geometry);
                AddTitle("Left Wheel", ' ');
                PrintProperties(axle.leftWheel);
                PrintProperties(axle.leftWheel.wheelController);
                AddTitle("Right Wheel", ' ');
                PrintProperties(axle.rightWheel);
                PrintProperties(axle.rightWheel.wheelController);
                count++;
                SavePage();
            }

            // Display
            SavePage();
            for (int i = 0; i < pages.Count && i < columns.Count; i++)
            {
                columns[i].text = pages[i];
            }

            text = "";
            lineCount = 0;
            pages.Clear();

        }

        private void PrintProperties(object obj)
        {
            foreach (var field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | 
                BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (!field.IsDefined(typeof(ShowInTelemetry), false)) continue;

                if (field.FieldType == typeof(float))
                {
                    string value = ((float)field.GetValue(obj)).ToString("0.00");
                    AddLine(field.Name, value);
                }
                else
                {
                    try
                    {
                        AddLine(field.Name, field.GetValue(obj).ToString());
                    }
                    catch { };
                }

                if (lineCount > linesPerColumn) SavePage();
            }

            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField |
                BindingFlags.GetProperty | BindingFlags.Instance))
            {
                if (!property.IsDefined(typeof(ShowInTelemetry), false)) continue;

                if (property.PropertyType == typeof(float))
                {
                    string value = ((float)property.GetValue(obj, null)).ToString("0.00");
                    AddLine(property.Name, value);
                }
                else
                {
                    try
                    {
                        AddLine(property.Name, (string)property.GetValue(obj, null).ToString());
                    }
                    catch { };
                }

                if (lineCount > linesPerColumn) SavePage();
            }
        }

        private void SavePage()
        {
            pages.Add(text);
            text = "";
            lineCount = 0;
        }

        private void AddLine(string name, string value)
        {
            name = Truncate(name, 16);
            text += String.Format("{0,-18}{1,12}", ChangeCase(name), value) + "\n";
            lineCount++;
        }

        private void AddLine(string name, float value)
        {
            string stringValue = value.ToString("0.0");
            AddLine(name, stringValue);
        }

        private void AddTitle(string title, char filler)
        {
            text += "\n" + CenterString(title, 30, filler);
            lineCount++;
        }

        private void AddSpace()
        {
            text += "\n";
            lineCount += 1;
        }

        private string CenterString(string stringToCenter, int totalLength, char filler)
        {
            return stringToCenter.PadLeft(((totalLength - stringToCenter.Length) / 2)
                                + stringToCenter.Length, filler)
                       .PadRight(totalLength, filler) + "\n";
        }

        public string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "..";
        }

        public static string ChangeCase(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }
    }

    [System.AttributeUsage(System.AttributeTargets.All)]
    public class ShowInTelemetry : System.Attribute
    {
    }
}

