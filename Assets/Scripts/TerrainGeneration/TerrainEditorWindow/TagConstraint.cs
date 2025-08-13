using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TagConstraint : ScriptableObject
{
    public string Name;
    // Key value pair holding other tags that can be placed next to this tag, with a weight value.
    // Weight values are all 1 if all options are equally weighted.
    //public bool weighted;
    //[SerializeField] public List<KeyValuePair<string, int>> options;
    // TODO: need something that determines which side of a module is being refered to...
    [Header("If all weights are 1, then the options are all equally weighted. \nIf a weight is zero it will be ignored.")]
    public List<NameWeightPair> Options;



    public TagConstraint(string name)
    {
        this.Name = name;
        this.Options = new List<NameWeightPair>();
    }


    public TagConstraint()
    {
        this.Name = "new";
        this.Options = new List<NameWeightPair>();
    }

    public override string ToString()
    {
        string output = this.Name + " with options:\n";

        foreach (NameWeightPair option in Options)
        {
            output += option.ToString();
        }

        return output;
    }


    // https://gamedev.stackexchange.com/questions/74393/how-to-edit-key-value-pairs-like-a-dictionary-in-unitys-inspector
    [System.Serializable]
    public class NameWeightPair
    {
        public string Name;
        public int Weight; // consider using decmals for weights and they all add up to one... good for math, bad for inputing

        public NameWeightPair(string pairName = "new", int weight = 1)
        {
            this.Name = pairName;
            this.Weight = weight;
        }

        public override string ToString()
        {
            return this.Name + " with weight " + this.Weight.ToString() + "\n";
        }
    }
}
