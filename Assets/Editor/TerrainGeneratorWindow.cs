using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System; // not sure if this is correct?


namespace UnityRoyale.EditorExtensions
{
    public class TerrainGeneratorWindow : EditorWindow
    {
        private static List<TagConstraint> ruleset = new List<TagConstraint>();
        private static string rulesetName;



        public static void ShowWindow(RulesetData inputedRuleset)
        {
            var window = GetWindow<TerrainGeneratorWindow>();
            window.titleContent = new GUIContent("Terrain Generator");
            window.minSize = new Vector2(300, 125);
            window.maxSize = new Vector2(300, 125);

            ruleset = inputedRuleset.ruleset;
            rulesetName = inputedRuleset.r_name;

        }

        private void OnEnable()
        {
            VisualTreeAsset original = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TerrainGeneratorWindow.uxml");
            TemplateContainer treeAsset = original.CloneTree();
            rootVisualElement.Add(treeAsset);

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/TerrainGeneratorWindow.uss");
            rootVisualElement.styleSheets.Add(styleSheet);

            GenerateTerrainButton();

            //TODO: fix this, has to do with the order of operations I think
            TextElement windowName = rootVisualElement.Query<TextElement>("ruleset-name");
            windowName.text = rulesetName;


        }

        private void GenerateTerrainButton()
        {
            Action openGenerateTerainWindow = () =>
            {
                TextField heightTextField = rootVisualElement.Query<TextField>("height-input");
                TextField widthTextField = rootVisualElement.Query<TextField>("width-input");
                try
                {
                    int height = Convert.ToInt32(heightTextField.value);
                    int width = Convert.ToInt32(widthTextField.value);
                    ValidateValues(height, width);
                    ValidateRuleset();
                    Debug.Log("Generating terrain with ruleset " + rulesetName + " and height, width values of " + height.ToString() + ", " + width.ToString() + ".");
                    // TODO: pass ruleset name for the log
                    ProceduralGeneration.Generate(ruleset, height, width);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                

            };

            Button generateTerrainButton = rootVisualElement.Query<Button>("generate-terrain-button");
            generateTerrainButton.RegisterCallback<MouseUpEvent>((evt) => openGenerateTerainWindow());

        }

        private void ValidateValues(int height, int width)
        {
            if (height <= 0 || width <= 0)
            {
                Debug.LogError("TerrainGeneratorWindow.cs/ValidateValues: height and width must be above zero.");
            }
            else
            {
                Debug.Log($"height: {height} and width: {width} are valid values.");
            }

        }

        // checks that every name in the list of contraints matches a tagconstraint name in the ruleset
        private void ValidateRuleset()
        {

        }
    }
}

