using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class Friction
{
    // Singleton instance.
    private static Friction instance;
    [SerializeField]
    public static Friction Instance
    {
        get
        {
            if (instance == null) instance = new Friction();

            instance.list["Default"] = Friction.Default;

            return instance;
        }
    }

    public static Friction Default
    {
        get
        {
            Friction defaultFriction = new Friction();
            defaultFriction.forward.Static = 0.7f;
            defaultFriction.forward.Rolling = 0.03f;
            defaultFriction.forward.Dynamic = 0.1f;

            defaultFriction.sideway.Static = 0.7f;
            defaultFriction.sideway.Rolling = 0.7f;
            defaultFriction.sideway.Dynamic = 0.3f;

            return defaultFriction;
        }
    }

    [SerializeField]
    public Dictionary<string, Friction> list = new Dictionary<string, Friction>();

    [SerializeField]
    public string name = "Default";

    // Forward
    [Header("Forward Coefficient")]
    public MyCoefficient forward = new MyCoefficient();

    // Sideways
    [Header("Sideway Coefficient")]
    public MyCoefficient sideway = new MyCoefficient();
    

    public Friction()
    {
        forward.Static = .7f;
        forward.Rolling = .03f;
        forward.Dynamic = .3f;

        sideway.Static = .7f;
        sideway.Rolling = .7f;
        sideway.Dynamic = .2f;
    }

    // Ensure that the instance is destroyed when the game is stopped in the editor.
    void OnApplicationQuit()
    {
        instance = null;
    }

    [System.Serializable]
    public class MyCoefficient
    {
        public float Static;
        public float Rolling;
        public float Dynamic;

        public MyCoefficient()
        {
            this.Static = 0.7f;
            this.Rolling = 0.001f;
            this.Dynamic = 0.3f;
        }
        public MyCoefficient(float Static, float Rolling, float Dynamic)
        {
            this.Static = Static;
            this.Rolling = Rolling;
            this.Dynamic = Dynamic;
        }
    }

}