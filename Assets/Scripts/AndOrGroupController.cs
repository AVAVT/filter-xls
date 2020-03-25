using System;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Outline))]
public class AndOrGroupController : MonoBehaviour, IConditionGroup
{
  GroupType _groupType = GroupType.AND_GROUP;
  public GroupType CurrentGroupType
  {
    get { return _groupType; }
    set
    {
      var color = value == GroupType.AND_GROUP ? andColor : orColor;
      groupTypeText.text = value == GroupType.AND_GROUP ? "AND GROUP" : "OR GROUP";
      groupTypeText.color = color;
      GetComponent<Outline>().effectColor = color;

      _groupType = value;
    }
  }

  public TMP_Text groupTypeText;
  public Button deleteButton;
  public Color andColor;
  public Color orColor;
  public RectTransform childContainer;
  public AndOrGroupController andOrGroupPrefab;
  public MatchGroupController matchGroupPrefab;

  public void Init(GroupType groupType)
  {
    CurrentGroupType = groupType;
    deleteButton.gameObject.SetActive(true);
  }

  public void AddAndGroup()
  {
    var newGroup = Instantiate(andOrGroupPrefab, childContainer);
    newGroup.Init(GroupType.AND_GROUP);
  }
  public void AddOrGroup()
  {
    var newGroup = Instantiate(andOrGroupPrefab, childContainer);
    newGroup.Init(GroupType.OR_GROUP);
  }
  public void AddMatchGroup()
  {
    Instantiate(matchGroupPrefab, childContainer);
  }

  public void DeleteSelf()
  {
    GameObject.Destroy(gameObject);
  }

  public void SwitchType()
  {
    CurrentGroupType = CurrentGroupType == GroupType.AND_GROUP ? GroupType.OR_GROUP : GroupType.AND_GROUP;
  }

  public Func<DataRow, bool> GetEvaluator()
  {
    List<Func<DataRow, bool>> childEvaluators = new List<Func<DataRow, bool>>();
    for (int i = 0; i < childContainer.childCount; i++)
    {
      childEvaluators.Add((childContainer.GetChild(i).GetComponent<IConditionGroup>()).GetEvaluator());
    }
    if (CurrentGroupType == GroupType.AND_GROUP)
    {
      return row =>
      {
        foreach (var evaluator in childEvaluators)
        {
          if (!evaluator(row)) return false;
        }
        return true;
      };
    }
    else
    {
      return row =>
      {
        foreach (var evaluator in childEvaluators)
        {
          if (evaluator(row)) return true;
        }
        return false;
      };
    }
  }
}

public enum GroupType
{
  AND_GROUP,
  OR_GROUP
}