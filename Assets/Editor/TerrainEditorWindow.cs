using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using System.ComponentModel;

//Documentation: https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-uxml-element-ListView.html
// https://docs.unity3d.com/ScriptReference/UIElements.ListView.html (old)
// https://docs.unity3d.com/2022.3/Documentation/ScriptReference/UIElements.ListView.html

//TODO: list doesnt update name when changed in window
//TODO: make generate terrain button that opens new window
//TODO: Need to manually select a ruleset first before doing anything or visuals bug out
//TODO: UI BUG: doesn't updates name until after a new ruleset is clicked
// TODO: UI BUG: doesn't update new ruleset added until window reloaded


namespace UnityRoyale.EditorExtensions
{
    public class TerrainEditorWindow : EditorWindow
    {
        private static int listItemHeight = 16;


        [MenuItem("Tools/Terrain Editor Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<TerrainEditorWindow>();
            window.titleContent = new GUIContent("Terrain Editor");
            window.minSize = new Vector2(800, 600);

        }

        private void OnEnable()
        {
            VisualTreeAsset original = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TerrainEditorWindow.uxml");
            TemplateContainer treeAsset = original.CloneTree();
            rootVisualElement.Add(treeAsset);

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/TerrainEditorStyles.uss");
            rootVisualElement.styleSheets.Add(styleSheet);

            CreateRulesetListView();
            GenerateTerrainButton();
        }

        private void GenerateTerrainButton()
        {
            Action openGenerateTerainWindow = () =>
            {
                ListView rulesetList = rootVisualElement.Query<ListView>("ruleset-list").First();
                RulesetData rulesetData = rulesetList.selectedItem as RulesetData;

                try
                {
                    List<TagConstraint> selectedRuleset = rulesetData.ruleset;
                    Debug.Log("Opening window to generate terrain with ruleset " + rulesetData.r_name + ".");
                    TerrainGeneratorWindow.ShowWindow(rulesetData);
                }
                catch (Exception e)
                {
                    Debug.Log("No ruleset selected to generate.");
                }
                

            };

            Button generateTerrainButton = rootVisualElement.Query<Button>("generate-terrain-button");
            generateTerrainButton.RegisterCallback<MouseUpEvent>((evt) => openGenerateTerainWindow());

        }


        private void CreateRulesetListView()
        {
            FindAllRulesets(out RulesetData[] rulesets);

            ListView rulesetList = rootVisualElement.Query<ListView>("ruleset-list").First();
            rulesetList.itemsSource = rulesets;
            rulesetList.makeItem = () => new Label();
            rulesetList.bindItem = (element, i) => (element as Label).text = rulesets[i].r_name;

            rulesetList.fixedItemHeight = listItemHeight;
            rulesetList.selectionType = SelectionType.Single;


            /// Uses the same code as selectionChanged, allowing for the user to go back to the current ruleset in the inspector without
            /// selecting another ruleset first
            rulesetList.itemsChosen += (enumerable) =>
            {
                foreach (object it in enumerable)
                {
                    Box rulesetInfoBox = rootVisualElement.Query<Box>("ruleset-info").First();
                    rulesetInfoBox.Clear();

                    RulesetData ruleset = it as RulesetData;

                    //Debug.Log("CreateRulesetListView: selected ruleset name: " + ruleset.r_name);


                    SerializedObject serializedRuleset = new SerializedObject(ruleset);
                    SerializedProperty rulesetProperty = serializedRuleset.GetIterator();
                    rulesetProperty.Next(true);

                    while (rulesetProperty.NextVisible(false))
                    {
                        PropertyField propertyField = new PropertyField(rulesetProperty);

                        // Disable the m_Script property so that no one can change the scripting reference
                        propertyField.SetEnabled(rulesetProperty.name != "m_Script");
                        propertyField.Bind(serializedRuleset);
                        rulesetInfoBox.Add(propertyField);

                    }
                }

                RulesetData selectedRuleset = enumerable.First() as RulesetData;
                CreateTagConstraintListView(selectedRuleset);

                rulesetList.RefreshItems();
            };

            rulesetList.selectionChanged += (enumerable) =>
            {
                foreach (object it in enumerable)
                {
                    Box rulesetInfoBox = rootVisualElement.Query<Box>("ruleset-info").First();
                    rulesetInfoBox.Clear();

                    RulesetData ruleset = it as RulesetData;
                    
                    //Debug.Log("CreateRulesetListView: selected ruleset name: " + ruleset.r_name);
                    

                    SerializedObject serializedRuleset = new SerializedObject(ruleset);
                    SerializedProperty rulesetProperty = serializedRuleset.GetIterator();
                    rulesetProperty.Next(true);

                    while (rulesetProperty.NextVisible(false))
                    {
                        PropertyField propertyField = new PropertyField(rulesetProperty);

                        // Disable the m_Script property so that no one can change the scripting reference
                        propertyField.SetEnabled(rulesetProperty.name != "m_Script");
                        propertyField.Bind(serializedRuleset);
                        rulesetInfoBox.Add(propertyField);

                    }
                }
                
                //Loads TC list for the selected ruleset
                RulesetData selectedRuleset = enumerable.First() as RulesetData;
                CreateTagConstraintListView(selectedRuleset);
                
                rulesetList.RefreshItems();
            };

            // TODO: fix, overwrites a ruleset?
            /// Overrides the add to list button for the ruleset list view
            rulesetList.Q<Button>("unity-list-view__add-button").clickable = new Clickable(() =>
            {
                Debug.Log("overriding the add button to properly initalize the ruleset scriptable object");

                RulesetCounter counter = (RulesetCounter)AssetDatabase.LoadAssetAtPath("Assets/Rulesets/Counter.asset", typeof(RulesetCounter));
                string fileName = "ruleset" + counter.rulesetCounter.ToString() + ".asset"; // plus the numerb of of how many there are in the folder
                string path = "Assets/Rulesets/" + fileName;

                var newRuleset = RulesetData.CreateInstance<RulesetData>();
                newRuleset.r_name = "new";
                newRuleset.ruleset = new List<TagConstraint>();

                AssetDatabase.CreateAsset(newRuleset, path);
                AssetDatabase.SaveAssets();
                counter.rulesetCounter++;

                //rulesetList.Rebuild();
                rulesetList.RefreshItems();
            });

            rulesetList.Rebuild(); // expensive, RefreshItems is less expensive (see unity listview documentation)

        }

        //TODO: on item edited in the scroll info panel, tagconstraintList.RefreshItems();
        // TODO: bug: list length too short and restricted
        private void CreateTagConstraintListView(RulesetData rulesetData)
        {
            List<TagConstraint> tagconstraints = rulesetData.ruleset;
            
            //Debug.Log("CTCLV: creating list for ruleset " + rulesetData.r_name + " with " + tagconstraints.Count + " tagconstratint(s).");
            
            ListView tagconstraintList = rootVisualElement.Query<ListView>("tagconstraint-list").First();
            tagconstraintList.itemsSource = tagconstraints;
            tagconstraintList.makeItem = () => new Label();
            tagconstraintList.bindItem = (element, i) =>
            {
                
                //Debug.Log("CTCLV: item " + i + " of " + tagconstraints.Count);

                if (tagconstraints.Count > 0)
                {
                    (element as Label).text = tagconstraints[i].Name; //sometimes still gives an error (TODO: the tag constraint that new isnt populating when  using the plus sign in the inspector (remove? or find the name of the button)
                } 
            };

            tagconstraintList.fixedItemHeight = listItemHeight;
            tagconstraintList.selectionType = SelectionType.Single;
            

            /// Same as the selectionChanged function
            tagconstraintList.itemsChosen += objects =>
            {
                foreach (object it in objects)
                {
                    //TODO: change ruleset-info name to tagconstriant-info to be more clear
                    Box rulesetInfoBox = rootVisualElement.Query<Box>("ruleset-info").First();
                    rulesetInfoBox.Clear();

                    TagConstraint tagconstraint = it as TagConstraint; // selected tagconstraint

                    SerializedObject serializedRuleset = new SerializedObject(tagconstraint); //issue line, probably because not init earlier
                    SerializedProperty rulesetProperty = serializedRuleset.GetIterator();
                    rulesetProperty.Next(true);

                    while (rulesetProperty.NextVisible(false))
                    {
                        PropertyField prop = new PropertyField(rulesetProperty);

                        // Disable the m_Script property so that no one can change the scripting reference
                        prop.SetEnabled(rulesetProperty.name != "m_Script");
                        prop.Bind(serializedRuleset);
                        rulesetInfoBox.Add(prop);

                    }
                }
            };

            //Show tagconstraint information
            tagconstraintList.selectionChanged += objects =>
            {
                foreach (object it in objects)
                {
                    //TODO: change ruleset-info name to tagconstriant-info to be more clear
                    Box rulesetInfoBox = rootVisualElement.Query<Box>("ruleset-info").First(); 
                    rulesetInfoBox.Clear();

                    TagConstraint tagconstraint = it as TagConstraint; // selected tagconstraint

                    SerializedObject serializedRuleset = new SerializedObject(tagconstraint); //issue line, probably because not init earlier
                    SerializedProperty rulesetProperty = serializedRuleset.GetIterator();
                    rulesetProperty.Next(true);

                    while (rulesetProperty.NextVisible(false))
                    {
                        PropertyField prop = new PropertyField(rulesetProperty);

                        // Disable the m_Script property so that no one can change the scripting reference
                        prop.SetEnabled(rulesetProperty.name != "m_Script");
                        prop.Bind(serializedRuleset);
                        rulesetInfoBox.Add(prop);

                    }
                }
            };

            // https://discussions.unity.com/t/how-to-customize-the-itemsadded-for-a-listview/857045/4
            // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/UIElements.ListView.html
            // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ScriptableObject.CreateInstance.html
            // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/CreateAssetMenuAttribute.html
            // https://unity.com/resources/create-modular-game-architecture-with-scriptable-objects-ebook?isGated=false
            


            //https://www.reddit.com/r/Unity3D/comments/qinx4s/creating_scriptable_objects_dynamically/
            //https://discussions.unity.com/t/how-to-customize-the-itemsadded-for-a-listview/857045/3
            /// Overrides the add to list button for the tagconstriant list view
            tagconstraintList.Q<Button>("unity-list-view__add-button").clickable = new Clickable(() =>
            {
                Debug.Log("overriding the add button to properly initalize the tagconstraint scriptable object");
                //make new tagcosntraint SO
                //add it to the selected ruleset
                //refresh

                RulesetCounter counter = (RulesetCounter)AssetDatabase.LoadAssetAtPath("Assets/Rulesets/Counter.asset", typeof(RulesetCounter));
                string fileName = "tagconstraint" + counter.tagconstraintCounter.ToString() + ".asset"; // plus the numerb of of how many there are in the folder
                //string fileName = "test.asset";
                string path = "Assets/Rulesets/Tagconstraints/" + fileName;

                var newTagconstraint = TagConstraint.CreateInstance<TagConstraint>();
                newTagconstraint.Name = "new";
                newTagconstraint.Options.Add(new TagConstraint.NameWeightPair("new", 1));

                AssetDatabase.CreateAsset(newTagconstraint, path);
                AssetDatabase.SaveAssets();
                counter.tagconstraintCounter++;


                //AssetDatabase.FindAssets
                // Check that this vv doesnt need ot be used
                //TagConstraint connectedTagconstraintSO = AssetDatabase.LoadAssetAtPath<TagConstraint>(fileName);
                ListView rulesetList = rootVisualElement.Query<ListView>("ruleset-list").First();
                RulesetData rulesetData = rulesetList.selectedItem as RulesetData;
                rulesetData.ruleset.Add(newTagconstraint);

                // go through all other tag constraints in the ruleset and add an option of the name?
                // would also need to have functionallity where when a name is changed, then the matching name changes too
                // not sure if I can access old names... like if someone selects the name box, keep name in a var
                // if text box is different from var when user clicks out of textbox then look for options matching var and change to 
                // new name too
                // https://discussions.unity.com/t/texteditor-in-editor-gui/64943/3


                tagconstraintList.RefreshItems();

            });

            tagconstraintList.Q<Button>("unity-list-view__remove-button").clickable = new Clickable(() => 
            {
                RulesetCounter counter = (RulesetCounter)AssetDatabase.LoadAssetAtPath("Assets/Rulesets/Counter.asset", typeof(RulesetCounter));
                
                TagConstraint selectedTagConstraint = tagconstraintList.selectedItem as TagConstraint;

                string fileName = selectedTagConstraint.name;
                string path = "Assets/Rulesets/Tagconstraints/" + fileName + ".asset";

                ListView rulesetList = rootVisualElement.Query<ListView>("ruleset-list").First();
                RulesetData rulesetData = rulesetList.selectedItem as RulesetData;
                rulesetData.ruleset.Remove(selectedTagConstraint);


                Debug.Log("Deleting " +  path);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();

                tagconstraintList.RefreshItems();

                // Don't reduce the count in the ruleset list, since it needs to be one above the number in the NAME not the actual count
                // of tagconstraints
            }); 
        }


        private void FindAllRulesets(out RulesetData[] rulesets)
        {
            var guids = AssetDatabase.FindAssets("t:RulesetData");

            rulesets = new RulesetData[guids.Length];

            for (int i = 0; i < guids.Length; i++) 
            { 
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                rulesets[i] = AssetDatabase.LoadAssetAtPath<RulesetData>(path);
            }
            Debug.Log("TerrainEditorWindow.cs/FindAllRulesets: found all rulesets");
        }
    }
}

