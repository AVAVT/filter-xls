using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;

public class PartitionGroupController : MonoBehaviour
{
  public TMP_Dropdown fieldNameDropDown;
  public TMP_InputField valueInput;

  public void DeleteSelf()
  {
    GameObject.Destroy(gameObject);
  }

  public Func<DataRow, string> GetEvaluator()
  {
    return row =>
    {
      var value = row.ItemArray[fieldNameDropDown.value].ToString().Trim();
      var matchValue = valueInput.text.Trim();

      if (value.StartsWith(matchValue)) return value;
      else return null;
    };
  }
}
