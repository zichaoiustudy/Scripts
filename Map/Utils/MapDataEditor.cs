using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapData))]
public class MapDataEditor : Editor
{
    private int selectedQ = 0;
    private int selectedR = 0;
    private Color selectedColor = Color.white;
    private float selectedHeight = 1f;
    private int selectedAltitude = 3;
    private float selectedScale = 1f;
    private string selectedTerrainType = "default";
    private string selectedSpecialFunction = "";
    private Material selectedMaterial;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        MapData mapData = (MapData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add New Hex Tile Data", EditorStyles.boldLabel);
        
        // Fixed layout for Q and R coordinates on separate lines
        EditorGUILayout.LabelField("Coordinates:", EditorStyles.boldLabel);
        selectedQ = EditorGUILayout.IntField("Q Coordinate:", selectedQ);
        selectedR = EditorGUILayout.IntField("R Coordinate:", selectedR);

        selectedColor = EditorGUILayout.ColorField("Color:", selectedColor);
        selectedHeight = EditorGUILayout.FloatField("Height:", selectedHeight);
        selectedAltitude = EditorGUILayout.IntField("Altitude:", selectedAltitude);
        selectedScale = EditorGUILayout.FloatField("Scale:", selectedScale);
        
        // Terrain type with explanation
        EditorGUILayout.LabelField("Terrain Type:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter a terrain type. If it matches a valid Unity tag, it will be applied as a tag. Common values: 'Unwalkable', 'Water', 'Mountain', etc.", MessageType.Info);
        selectedTerrainType = EditorGUILayout.TextField("Type:", selectedTerrainType);
        
        // Special function with explanation
        EditorGUILayout.LabelField("Special Function:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter a special function for gameplay mechanics. Examples: 'PlayerSpawn', 'MonsterSpawn', 'Treasure', etc.", MessageType.Info);
        selectedSpecialFunction = EditorGUILayout.TextField("Function:", selectedSpecialFunction);
        
        selectedMaterial = (Material)EditorGUILayout.ObjectField("Override Material:", selectedMaterial, typeof(Material), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Hex Tile Data"))
        {
            // Create a new SerializableHexTile instead of HexTile
            HexTileData newTile = new HexTileData(selectedQ, selectedR);
            newTile.color = selectedColor;
            newTile.height = selectedHeight;
            newTile.altitude = selectedAltitude;
            newTile.scale = selectedScale;
            newTile.terrainType = selectedTerrainType;
            newTile.specialFunction = selectedSpecialFunction;
            
            // SerializableHexTile doesn't have overrideMaterial property in your current implementation
            // If you want to add it, you'll need to modify SerializableHexTile class

            mapData.AddHexTile(newTile);
            EditorUtility.SetDirty(mapData);
        }

        if (GUILayout.Button("Clear All Tile Data"))
        {
            if (EditorUtility.DisplayDialog("Clear Tile Data", 
                "Are you sure you want to clear all hex tile data?", 
                "Yes", "No"))
            {
                mapData.ClearHexTiles();
                EditorUtility.SetDirty(mapData);
            }
        }
    }
}