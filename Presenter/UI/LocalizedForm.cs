﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Globalization;
using Pbp.Properties;

namespace Pbp.UI
{
    public class LocalizableForm : Form
    {
        /// <summary>
        /// Change language at runtime in the specified form
        /// </summary>
        protected void SetLanguage(Form form, CultureInfo lang)
        {
            //Set the language in the application
            System.Threading.Thread.CurrentThread.CurrentUICulture = lang;

            ComponentResourceManager resources = new ComponentResourceManager(form.GetType());

            if (form.MainMenuStrip != null)
            {
                ApplyResourceToControl(resources, form.MainMenuStrip, lang);
            }
            ApplyResourceToControl(resources, form, lang);

            //resources.ApplyResources(form, "$this", lang);
            form.Text = resources.GetString("$this.Text", lang);

            Settings.Default.SelectedCulture = lang.Name;
        }

        protected void ApplyResourceToControl(ComponentResourceManager resources, Control control, CultureInfo lang)
        {
            foreach (Control c in control.Controls)
            {
                ApplyResourceToControl(resources, c, lang);
                //resources.ApplyResources(c, c.Name, lang);
                string text = resources.GetString(c.Name + ".Text", lang);
                if (text != null)
                {
                    c.Text = text;
                }
                if (c.GetType() == typeof(Pbp.Components.CustomGroupBox))
                {
                    string title = resources.GetString(c.Name + ".Title", lang);
                    if (title != null)
                    {
                        ((Pbp.Components.CustomGroupBox)c).Title = title;
                    }                
                }
                else if (c.GetType() == typeof(Pbp.Components.SearchTextBox))
                {
                    string title = resources.GetString(c.Name + ".PlaceHolderText", lang);
                    if (title != null)
                    {
                        ((Pbp.Components.SearchTextBox)c).PlaceHolderText = title;
                    }
                }
            }
        }

        protected void ApplyResourceToControl(ComponentResourceManager resources, MenuStrip menu, CultureInfo lang)
        {
            foreach (ToolStripItem m in menu.Items)
            {
                //resources.ApplyResources(m, m.Name, lang);
                string text = resources.GetString(m.Name + ".Text", lang);
                if (text != null)
                {
                    m.Text = text;
                }
                if (m.GetType() == typeof(ToolStripMenuItem))
                {
                    foreach (var d in ((ToolStripMenuItem)m).DropDownItems)
                    {
                        if (d.GetType() == typeof(ToolStripMenuItem))
                        {
                            ApplyResourceToControl(resources, (ToolStripMenuItem)d, lang);
                        }
                    }
                }
            }
        }
        protected void ApplyResourceToControl(ComponentResourceManager resources, ToolStripMenuItem menu, CultureInfo lang)
        {
            string text = resources.GetString(menu.Name + ".Text", lang);
            if (text != null)
            {
                menu.Text = text;
            }
            foreach (var d in ((ToolStripMenuItem)menu).DropDownItems)
            {
                if (d.GetType() == typeof(ToolStripMenuItem))
                {
                    ApplyResourceToControl(resources, (ToolStripMenuItem)d, lang);
                }
            }
        }


    }
}