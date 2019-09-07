using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(HexGrid))]
public class HexMapManager : MonoBehaviour
{
    public int mapVersion = 3;
    public string mapFilePath;
    public bool loadOnStart = true;

    HexGrid hexGrid;

    private void Awake()
    {
        hexGrid = GetComponent<HexGrid>();
    }

    private void Start()
    {
        Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
        if (loadOnStart && !string.IsNullOrEmpty(mapFilePath))
        {
            Load(mapFilePath);
            hexGrid.AddUnit(
                Instantiate(HexUnit.unitPrefab), 
                hexGrid.GetCell(new HexCoordinate(hexGrid.cellCountX / 2, hexGrid.cellCountZ / 2)), 
                0);
        }
        
    }

    private void Update()
    {
        
    }

    #region Save/Load methods
    public void Save(string path)
    {
        //string path = Path.Combine(Application.persistentDataPath, "test.map");
        using (BinaryWriter writer =
            new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(mapVersion);
            hexGrid.Save(writer);
        }

    }

    public void Load(string path)
    {
        //string path = Path.Combine(Application.persistentDataPath, "test.map");
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            return;
        }
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if (header <= mapVersion)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
    #endregion
}
