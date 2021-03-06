﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JetBrains.Annotations;
using Panda.Core.Internal;
using Panda.UI.Properties;

namespace Panda.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
#if DEBUG
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Trace.Listeners.Add(new ConsoleTraceListener());
#endif
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, string.Format("{0} in Panda.UI", e.Exception.GetType().Name),
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        public static bool IsValid(DependencyObject obj)
        {
            // The dependency object is valid if it has no errors, 
            //and all of its children (that are dependency objects) are error-free.
            return !Validation.GetHasError(obj) &&
                LogicalTreeHelper.GetChildren(obj)
                    .OfType<DependencyObject>()
                    .All(IsValid);
        }

        [NotNull]
        public static IEnumerable<DependencyObject> VisualChildren(DependencyObject visual)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(visual);
            for (var i = 0; i < childrenCount; i++)
            {
                yield return VisualTreeHelper.GetChild(visual, i);
            }
        }

        [NotNull]
        public static IEnumerable<DependencyObject> VisualDescendants(DependencyObject parent)
        {
            return VisualChildren(parent).Append(VisualChildren(parent).SelectMany(VisualDescendants));
        }

        [NotNull]
        public static T FindVisualChild<T>(DependencyObject visual, object toFind) where T : FrameworkElement
        {
            return VisualDescendants(visual).OfType<T>().First(fe => ReferenceEquals(fe.DataContext, toFind));
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
