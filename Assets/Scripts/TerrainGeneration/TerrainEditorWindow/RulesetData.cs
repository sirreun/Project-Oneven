using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newrulesetdata.asset", menuName = "ScriptableObjects/Ruleset")]
public class RulesetData : ScriptableObject
{
    public string r_name;

    [SerializeField] public List<TagConstraint> ruleset;
 

   public RulesetData(List<TagConstraint> iRuleset, string iName = "new")
   {
        this.r_name = iName;
        this.ruleset = iRuleset;
   }

}



/*https://stackoverflow.com/questions/56106179/create-a-scriptable-object-instance-through-a-constructor
*/
