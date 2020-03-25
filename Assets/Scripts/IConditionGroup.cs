using System;
using System.Data;

interface IConditionGroup
{
  Func<DataRow, bool> GetEvaluator();
}