using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.xUnit.Test;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; private set; } = priority;
}
