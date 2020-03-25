using UnityEngine;
using System.IO;
using System.Collections;
using System;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelDataReader;
using System.Data;
using SwiftExcel;
using SFB;

public class XLSXLoader : MonoBehaviour
{
  public CanvasGroup folderGroup;
  public CanvasGroup conditionGroup;
  public TMP_Text inputPathText;
  public TMP_Text outputPathText;
  public UnityEngine.UI.Button filterButton;
  public UnityEngine.UI.Button partitionButton;
  public UnityEngine.UI.Button filterToggleButton;
  public UnityEngine.UI.Button partitionToggleButton;
  public LogPanelController logPanel;
  public AndOrGroupController mainConditionGroup;
  public PartitionGroupController partitionConditionGroup;

  private string _inputText = "";
  public string InputPath
  {
    get { return _inputText; }
    set
    {
      _inputText = value;
      inputPathText.text = value;
      if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(OutputPath))
      {
        filterButton.interactable = true;
        partitionButton.interactable = true;
      }
    }
  }

  private string _outputText = "";
  public string OutputPath
  {
    get { return _outputText; }
    set
    {
      _outputText = value;
      outputPathText.text = value;
      if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(InputPath))
      {
        filterButton.interactable = true;
        partitionButton.interactable = true;
      }
    }
  }

  public enum State
  {
    SETUP,
    WORKING
  }

  State _currentState = State.SETUP;
  public State CurrentState
  {
    get { return _currentState; }
    set
    {
      folderGroup.interactable = value == State.SETUP;
      conditionGroup.interactable = value == State.SETUP;
      filterButton.gameObject.SetActive(value == State.SETUP && CurrentMode == Mode.FILTER);
      partitionButton.gameObject.SetActive(value == State.SETUP && CurrentMode == Mode.PARTITION);
      _currentState = value;
    }
  }

  public enum Mode : int
  {
    FILTER = 0,
    PARTITION = 1
  }

  public Color modeActiveColor;
  public Color modeInactiveColor;

  Mode _currentMode = Mode.FILTER;
  public Mode CurrentMode
  {
    get { return _currentMode; }
    set
    {
      filterButton.gameObject.SetActive(value == Mode.FILTER);
      partitionButton.gameObject.SetActive(value == Mode.PARTITION);
      mainConditionGroup.gameObject.SetActive(value == Mode.FILTER);
      partitionConditionGroup.gameObject.SetActive(value == Mode.PARTITION);
      filterToggleButton.image.color = value == Mode.FILTER ? modeActiveColor : modeInactiveColor;
      partitionToggleButton.image.color = value == Mode.PARTITION ? modeActiveColor : modeInactiveColor;
      _currentMode = value;
    }
  }

  List<string> filesToProcess;
  Func<DataRow, bool> filterEvaluator;
  Func<DataRow, string> partitionEvaluator;

  private void Start()
  {
    // InputPath = "E:\\Work\\Dan\\filter-excel-test\\Input";
    // OutputPath = "E:\\Work\\Dan\\filter-excel-test\\Output";

    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    mainConditionGroup.AddMatchGroup();
  }
  public void OnClick_SetMode(int mode)
  {
    CurrentMode = (Mode)mode;
  }
  public void OnClick_FilterButton()
  {
    DoFilterOrPartition();
  }

  public void OnClick_PartitionButton()
  {
    DoFilterOrPartition();
  }

  public void OnClick_InputPath()
  {
    var folder = ChooseFolder();
    if (string.IsNullOrEmpty(folder)) return;
    else InputPath = folder;
  }

  public void OnClick_OutputPath()
  {
    var folder = ChooseFolder();
    if (string.IsNullOrEmpty(folder)) return;
    else OutputPath = folder;
  }

  private string ChooseFolder()
  {
    var paths = StandaloneFileBrowser.OpenFolderPanel("Choose Folder", "", false);
    if (paths.Length > 0) return paths[0];
    else return "";
  }

  private void DoFilterOrPartition()
  {
    CurrentState = State.WORKING;

    if (CurrentMode == Mode.FILTER)
    {
      filterEvaluator = mainConditionGroup.GetEvaluator();
    }
    else
    {
      partitionEvaluator = partitionConditionGroup.GetEvaluator();
    }

    filesToProcess = GetXLSXFileNames(Directory.GetFiles(InputPath));

    List<ItemInfo> itemInfos = new List<ItemInfo>();
    foreach (var filePath in filesToProcess)
    {
      var fileInfo = new FileInfo(filePath);
      itemInfos.Add(new ItemInfo
      {
        eta = (long)(fileInfo.Length * 0.006),
        fileName = fileInfo.Name
      });
    }
    logPanel.PopulateList(itemInfos);

    if (CurrentMode == Mode.FILTER)
    {
      FilterNextFile();
    }
    else
    {
      PartitionNextFile();
    }
  }

  void PartitionNextFile()
  {
    if (filesToProcess.Count > 0)
    {
      var filePath = filesToProcess[0];
      filesToProcess.RemoveAt(0);

      var fileInfo = new FileInfo(filePath);
      Debug.Log($"Partitioning file {filePath}");
      Debug.Log($"File size: {(fileInfo.Length / (1024f * 1024f)).ToString("0.00")} MB");

      Task.Run(() => PartitionFile(fileInfo));
    }
    else
    {
      CurrentState = State.SETUP;
      Debug.Log("Done.");
    }
  }

  void FilterNextFile()
  {
    if (filesToProcess.Count > 0)
    {
      var filePath = filesToProcess[0];
      filesToProcess.RemoveAt(0);

      var fileInfo = new FileInfo(filePath);
      Debug.Log($"Filtering file {filePath}");
      Debug.Log($"File size: {(fileInfo.Length / (1024f * 1024f)).ToString("0.00")} MB");

      Task.Run(() => FilterFile(fileInfo));
    }
    else
    {
      CurrentState = State.SETUP;
      Debug.Log("Done.");
    }
  }

  private List<string> GetXLSXFileNames(string[] filePaths)
  {
    var result = new List<string>();
    foreach (var filePath in filePaths)
    {
      if (Path.GetExtension(filePath) == ".xlsx") result.Add(filePath);
    }

    return result;
  }

  private void PartitionFile(FileInfo fileInfo)
  {
    var data = ReadXLSX(fileInfo);
    if (data != null)
    {
      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          logPanel.UpdateState(fileInfo.Name, ItemProgressState.PROCESSING);
        });
      var results = PartitionRows(data.Tables[0]);

      WriteXLSXParition(results, fileInfo);

    }
    Debug.Log("=====");
  }

  private void FilterFile(FileInfo fileInfo)
  {
    var data = ReadXLSX(fileInfo);
    if (data != null)
    {
      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          logPanel.UpdateState(fileInfo.Name, ItemProgressState.PROCESSING);
        });
      var result = FilterRows(data.Tables[0]);
      WriteXLSXFilter(result, fileInfo);
    }
    Debug.Log("=====");
  }

  private DataSet ReadXLSX(FileInfo fileInfo)
  {
    UnityMainThreadDispatcher.Instance().Enqueue(() =>
    {
      logPanel.UpdateState(fileInfo.Name, ItemProgressState.READING);
    });

    var sw = new System.Diagnostics.Stopwatch();
    DataSet result = null;
    try
    {
      sw.Start();
      using (var stream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read))
      {
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
          result = reader.AsDataSet();
        }
      }
      sw.Stop();
      Debug.Log($"Read Time: {sw.Elapsed}");
      Debug.Log($"Speed: {sw.ElapsedMilliseconds / (float)fileInfo.Length}");
    }
    catch (Exception ex)
    {
      Debug.LogError(ex);
      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          logPanel.UpdateState(fileInfo.Name, ItemProgressState.ERROR);
          FilterNextFile();
        });
    }

    return result;
  }

  private Dictionary<string, List<DataRow>> PartitionRows(DataTable data)
  {
    var results = new Dictionary<string, List<DataRow>>();

    for (int i = 1; i < data.Rows.Count; i++)
    {
      DataRow row = data.Rows[i];
      var evalResult = partitionEvaluator(row);
      if (evalResult == null) continue;

      if (!results.ContainsKey(evalResult))
      {
        results.Add(evalResult, new List<DataRow>() { data.Rows[0] });
      }
      results[evalResult].Add(row);
    }

    return results;
  }

  private DataTable FilterRows(DataTable data)
  {
    for (int i = 1; i < data.Rows.Count; i++)
    {
      DataRow row = data.Rows[i];
      if (!filterEvaluator(row)) row.Delete();
    }
    data.AcceptChanges();

    return data;
  }

  private void WriteXLSXParition(Dictionary<string, List<DataRow>> data, FileInfo fileInfo)
  {
    UnityMainThreadDispatcher.Instance().Enqueue(() =>
    {
      logPanel.UpdateState(fileInfo.Name, ItemProgressState.WRITING);
    });

    // var sw = new System.Diagnostics.Stopwatch();
    try
    {
      foreach (var result in data)
      {
        using (var ew = new ExcelWriter(Path.Combine(OutputPath, $"{result.Key} - {fileInfo.Name}")))
        {
          for (var rowNo = 0; rowNo < result.Value.Count; rowNo++)
          {
            var row = result.Value[rowNo];
            for (var colNo = 0; colNo < row.ItemArray.Length; colNo++)
            {
              ew.Write(row.ItemArray[colNo].ToString(), colNo + 1, rowNo + 1);
            }
          }
        }
      }
      // sw.Start();

      // sw.Stop();

      // Debug.Log($"Write Time: {sw.Elapsed}");
      // Debug.Log($"Speed: {sw.ElapsedMilliseconds / (float)fileInfo.Length}");
      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          logPanel.UpdateState(fileInfo.Name, ItemProgressState.DONE);
          PartitionNextFile();
        });
    }
    catch (Exception ex)
    {
      Debug.LogError(ex);
      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          logPanel.UpdateState(fileInfo.Name, ItemProgressState.ERROR);
          PartitionNextFile();
        });
    }
  }

  private void WriteXLSXFilter(DataTable table, FileInfo fileInfo)
  {
    UnityMainThreadDispatcher.Instance().Enqueue(() =>
    {
      logPanel.UpdateState(fileInfo.Name, ItemProgressState.WRITING);
    });

    // var sw = new System.Diagnostics.Stopwatch();
    try
    {
      // sw.Start();
      using (var ew = new ExcelWriter(Path.Combine(OutputPath, fileInfo.Name)))
      {
        for (var rowNo = 0; rowNo < table.Rows.Count; rowNo++)
        {
          var row = table.Rows[rowNo];
          for (var colNo = 0; colNo < row.ItemArray.Length; colNo++)
          {
            ew.Write(row.ItemArray[colNo].ToString(), colNo + 1, rowNo + 1);
          }
        }
      }
      // sw.Stop();

      // Debug.Log($"Write Time: {sw.Elapsed}");
      // Debug.Log($"Speed: {sw.ElapsedMilliseconds / (float)fileInfo.Length}");
      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          logPanel.UpdateState(fileInfo.Name, ItemProgressState.DONE);
          FilterNextFile();
        });
    }
    catch (Exception ex)
    {
      Debug.LogError(ex);
      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          logPanel.UpdateState(fileInfo.Name, ItemProgressState.ERROR);
          FilterNextFile();
        });
    }

  }
}