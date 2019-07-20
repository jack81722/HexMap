using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;

    public Text menuLabel, actionButtonLabel;
    public InputField nameInput;

    public RectTransform listContent;
    public SaveLoadItem itemPrefab;

    private bool saveMode;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;
        if (saveMode)
        {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else
        {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }
        fillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    string getSelectedPath()
    {
        string mapName = nameInput.text;
        if(mapName.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    #region Save/Load methods
    public void Save(string path)
    {
        //string path = Path.Combine(Application.persistentDataPath, "test.map");
        using (BinaryWriter writer =
            new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(1);
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
            if (header <= 1)
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

    public void Action()
    {
        string path = getSelectedPath();
        if (path == null)
            return;
        if (saveMode)
            Save(path);
        else
            Load(path);
        Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    public void SelectItem(string name)
    {
        nameInput.text = name;
    }

    private void fillList()
    {
        for(int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }
        string[] paths =
            Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for(int i = 0; i < paths.Length; i++)
        {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }
        nameInput.text = "";
    }

    public void Delete()
    {
        string path = getSelectedPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        nameInput.text = "";
        fillList();
    }
}
