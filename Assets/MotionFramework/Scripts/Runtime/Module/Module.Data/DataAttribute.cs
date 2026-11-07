using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionFramework.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DataAttribute : Attribute
    {
        public Type DataType;
        public bool IsSave;
        public string LocalDataKey;
        public int Priority;

        public DataAttribute(Type dataType, bool isSave, int priority = 0)
		{
            DataType = dataType;
            IsSave = isSave;
            LocalDataKey = "TanTan_LocalData_" + DataType;
            Priority = priority;
		}
    }
}
