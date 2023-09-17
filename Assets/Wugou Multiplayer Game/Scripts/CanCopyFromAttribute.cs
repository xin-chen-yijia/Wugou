using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CanCopyFromAttribute : Attribute
{
    public string component;

    public CanCopyFromAttribute(string component)
    {
        this.component = component;
    }
}
