using System.Collections.Generic;
using UnityEngine;

public class LogPanelController : MonoBehaviour
{
  public Transform list;
  public LogItemController itemPrefab;

  Dictionary<string, LogItemController> logItems = new Dictionary<string, LogItemController>();

  public void PopulateList(List<ItemInfo> items)
  {
    CleanList();
    foreach (var item in items)
    {
      var newLogItem = Instantiate(itemPrefab, list);
      newLogItem.Init(item);
      logItems.Add(item.fileName, newLogItem);
    }
  }

  public void UpdateState(string fileName, ItemProgressState state)
  {
    logItems[fileName].CurrentState = state;
  }

  void CleanList()
  {
    foreach (var kvp in logItems)
    {
      GameObject.Destroy(kvp.Value.gameObject);
    }
    logItems = new Dictionary<string, LogItemController>();
  }
}