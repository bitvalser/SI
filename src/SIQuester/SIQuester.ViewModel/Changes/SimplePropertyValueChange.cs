﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SIQuester
{
    internal sealed class SimplePropertyValueChange: IChange
    {
        public object Element { get; set; }
        public string PropertyName { get; set; }
        public object Value { get; set; }

        #region IChange Members

        public void Undo()
        {
            var type = Element.GetType();
            var property = type.GetProperty(PropertyName);

            var oldValue = property.GetValue(Element, null);
            property.SetValue(Element, Value, null);

            Value = oldValue;
        }

        public void Redo() => Undo();

        #endregion
    }
}
