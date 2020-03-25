using System;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchGroupController : MonoBehaviour, IConditionGroup
{
  public TMP_Dropdown fieldNameDropDown;
  public TMP_Dropdown matchTypeDropdown;
  public TMP_InputField valueInput;

  public void DeleteSelf()
  {
    GameObject.Destroy(gameObject);
  }

  public Func<DataRow, bool> GetEvaluator()
  {
    return row =>
    {
      var value = row.ItemArray[fieldNameDropDown.value].ToString().Trim();
      var matchValue = valueInput.text.Trim();

      switch (matchTypeDropdown.value)
      {
        case 1: return value == matchValue;
        case 2: return value.StartsWith(matchValue);
        case 0: default: return value.Contains(matchValue);
      }
    };
  }
}