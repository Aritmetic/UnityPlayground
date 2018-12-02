using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWH.WheelController3D
{
    public partial class WheelController
    {
        /// <summary>
        /// Container class for holding wheel friction presets.
        /// </summary>
        [System.Serializable]
        public class FrictionPreset
        {
            public FrictionPreset(string name, Vector4 BCDE)
            {
                this.name = name;
                this.BCDE = BCDE;
                curve = GenerateFrictionCurve(BCDE);

                if (FrictionPresetList == null) FrictionPresetList = new List<FrictionPreset>();
                FrictionPresetList.Add(this);
            }

            public string name;
            public Vector4 BCDE;
            [SerializeField]
            private AnimationCurve curve;
            public AnimationCurve Curve
            {
                get
                {
                    return curve;
                }
            }


            /* PRESETS
             * Add new presets here. Do not forget to add them to the FrictionPresetEnum below too or else they will not show in the editor dropdown.
             * Presets must be listed in the same order below as in the enum.
            */
            public static FrictionPreset TarmacDry = new FrictionPreset("TarmacDry", new Vector4(12.5f, 2.05f, 0.92f, 0.97f));
            public static FrictionPreset TarmacWet = new FrictionPreset("TarmacWet", new Vector4(13.2f, 2.35f, 0.82f, 1.0f));
            public static FrictionPreset Gravel = new FrictionPreset("Gravel", new Vector4(9.0f, 1.1f, 0.8f, 1f));
            public static FrictionPreset Grass = new FrictionPreset("Grass", new Vector4(8.4f, 1.3f, 0.5f, 0.4f));
            public static FrictionPreset Sand = new FrictionPreset("Sand", new Vector4(8.0f, 1.2f, 0.6f, 0.5f));
            public static FrictionPreset Snow = new FrictionPreset("Snow", new Vector4(8.5f, 1.1f, 0.4f, 0.9f));
            public static FrictionPreset Ice = new FrictionPreset("Ice", new Vector4(4.0f, 2.0f, 0.1f, 1.0f));
            public static FrictionPreset Generic = new FrictionPreset("Generic", new Vector4(8.0f, 1.9f, 0.8f, 0.99f));
            public static FrictionPreset Tracks = new FrictionPreset("Tracks", new Vector4(0.1f, 2f, 15f, 1f));
            public static FrictionPreset Arcade = new FrictionPreset("Arcade", new Vector4(4f, 1f, 2f, 0.5f));

            public enum FrictionPresetEnum { TarmacDry, TarmacWet, Gravel, Grass, Sand, Snow, Ice, Generic, Tracks, Arcade };

            [SerializeField]
            public static List<FrictionPreset> FrictionPresetList;

            /// <summary>
            /// Generate Curve from B,C,D and E parameters of Pacejka's simplified magic formula
            /// </summary>
            public static AnimationCurve GenerateFrictionCurve(Vector4 p)
            {
                AnimationCurve ac = new AnimationCurve();
                Keyframe[] frames = new Keyframe[60];
                for (int i = 0; i < frames.Length; i++)
                {
                    float t = i / 59.0f;
                    float v = GetFrictionValue(t, p);
                    ac.AddKey(t, v);
                }
                return ac;
            }

            private static float GetFrictionValue(float slip, Vector4 p)
            {
                var B = p.x;
                var C = p.y;
                var D = p.z;
                var E = p.w;
                var t = Mathf.Abs(slip);
                return D * Mathf.Sin(C * Mathf.Atan(B * t - E * (B * t - Mathf.Atan(B * t))));
            }
        }
    }
}
