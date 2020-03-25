using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class LogItemController : MonoBehaviour
{
  public RectTransform progressCol;
  public RectTransform progressBar;
  public TMP_Text etaText;
  public TMP_Text progressText;
  public TMP_Text fileNameText;
  public Color inProgressColor;
  public Color errorColor;
  public Color completedColor;

  long currentTime = 0;
  long eta = 0;
  ItemProgressState _state = ItemProgressState.PENDING;
  public ItemProgressState CurrentState
  {
    get { return _state; }
    set
    {
      if (value == ItemProgressState.PENDING)
      {
        currentTime = 0;
        progressText.text = "Pending";
        progressBar.GetComponent<Image>().color = inProgressColor;
      }
      else if (value == ItemProgressState.READING)
      {
        progressText.text = "Reading File...";
      }
      else if (value == ItemProgressState.PROCESSING)
      {
        progressText.text = "Filtering Results...";
      }
      else if (value == ItemProgressState.WRITING)
      {
        progressText.text = "Writing Output...";
      }
      else if (value == ItemProgressState.ERROR)
      {
        progressText.text = "## ERROR ##";
        progressBar.GetComponent<Image>().color = errorColor;
        progressBar.sizeDelta = new Vector2(progressCol.sizeDelta.x, progressBar.sizeDelta.y);
      }
      else if (value == ItemProgressState.DONE)
      {
        progressText.text = "";
        progressBar.GetComponent<Image>().color = completedColor;
        etaText.text = "Done";
        progressBar.sizeDelta = new Vector2(progressCol.sizeDelta.x, progressBar.sizeDelta.y);
      }

      _state = value;
    }
  }
  public void Init(ItemInfo itemInfo)
  {
    this.eta = itemInfo.eta;
    UpdateETA();
    fileNameText.text = itemInfo.fileName;
    CurrentState = ItemProgressState.PENDING;
  }

  private void Update()
  {
    if (CurrentState == ItemProgressState.PENDING || CurrentState == ItemProgressState.DONE || CurrentState == ItemProgressState.ERROR) return;

    currentTime += (long)(Time.deltaTime * 1000);
    UpdateETA();
  }

  void UpdateETA()
  {
    long remainingTime = eta - currentTime;
    TimeSpan t = TimeSpan.FromMilliseconds(remainingTime);
    etaText.text = string.Format(
      "{0:D2}:{1:D2}:{2:D2}",
      t.Hours,
      t.Minutes,
      t.Seconds
    );

    float progressRatio = eta == 0 ? 1 : Mathf.Clamp((float)((double)currentTime / eta), 0, 1);
    progressBar.sizeDelta = new Vector2(progressCol.sizeDelta.x * progressRatio, progressBar.sizeDelta.y);
  }
}

public struct ItemInfo
{
  public long eta;
  public string fileName;
}

public enum ItemProgressState
{
  PENDING,
  READING,
  PROCESSING,
  WRITING,
  ERROR,
  DONE
}