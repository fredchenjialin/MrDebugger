﻿using System.Windows;
using System.Windows.Media;

namespace MrDebugger
{
    class WpfTreeHelper
    {
        public static T FindChild<T>(DependencyObject node)
            where T : DependencyObject
        {
            if (node == null)
                return null;

            T found = null;
            var childlen = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < childlen; i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                var target = child as T;
                if (target == null)
                {
                    found = FindChild<T>(child);
                    if (found != null)
                        break;
                }
                else
                {
                    found = (T)child;
                    break;
                }
            }

            return found;
        }
    }
}
