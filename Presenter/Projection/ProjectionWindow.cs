﻿/*
 *   PraiseBase Presenter
 *   The open source lyrics and image projection software for churches
 *
 *   http://praisebase.org
 *
 *   This program is free software; you can redistribute it and/or
 *   modify it under the terms of the GNU General Public License
 *   as published by the Free Software Foundation; either version 2
 *   of the License, or (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program; if not, write to the Free Software
 *   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 *
 */

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PraiseBase.Presenter.Properties;

namespace PraiseBase.Presenter.Projection
{
    public partial class ProjectionWindow : Form
    {
        protected Dictionary<int, Image> CurrentLayerImages;

        public ProjectionWindow(Screen projScreen)
        {
            InitializeComponent();
            CurrentLayerImages = new Dictionary<int, Image>();

            AssignToScreen(projScreen);

            var wpc = new ProjectionControl
            {
                ProjectionBackgroundColor = Settings.Default.ProjectionBackColor
            };
            projectionControlHost.Child = wpc;
        }

        /// <summary>
        /// Assigns the window to a screen's coordinates
        /// </summary>
        /// <param name="projScreen"></param>
        public void AssignToScreen(Screen projScreen)
        {
            Location = projScreen.WorkingArea.Location;
            Size = new Size(projScreen.WorkingArea.Width, projScreen.WorkingArea.Height);
        }

        /// <summary>
        /// Set to blackout
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="animate"></param>
        public void SetBlackout(bool enable, bool animate)
        {
            ((ProjectionControl)(projectionControlHost.Child)).BlackOut(enable, (animate ? Settings.Default.ProjectionFadeTime : 0));
        }

        /// <summary>
        /// Display text layer with transition
        /// </summary>
        /// <param name="layerNum"></param>
        /// <param name="layerContents"></param>
        /// <param name="fadetime"></param>
        public void DisplayLayer(int layerNum, Bitmap bmp, int fadetime)
        {
            if (layerNum == 2)
            {
                ((ProjectionControl)(projectionControlHost.Child)).SetProjectionText(bmp, fadetime);
            }
            else
            {
                ((ProjectionControl)(projectionControlHost.Child)).SetProjectionImage(bmp, fadetime);
            }
            CurrentLayerImages[layerNum] = bmp;
        }

        /// <summary>
        /// Hide layer with transition
        /// </summary>
        /// <param name="layerNum"></param>
        /// <param name="fadetime"></param>
        public void HideLayer(int layerNum, int fadetime)
        {
            var bmp = new Bitmap(Width, Height);

            if (layerNum == 2)
            {
                ((ProjectionControl)(projectionControlHost.Child)).SetProjectionText(bmp, fadetime);
            }
            else if (layerNum == 1)
            {
                ((ProjectionControl)(projectionControlHost.Child)).SetProjectionImage(bmp, fadetime);
            }

            CurrentLayerImages[layerNum] = bmp;            
        }

        /// <summary>
        /// Create preview image
        /// </summary>
        /// <returns></returns>
        public Image GetPreviewImage()
        {
            Image frame = new Bitmap(Width, Height);
            Graphics gr = Graphics.FromImage(frame);
            
            int usedLayers = 0;
            int i = 0;
            while (true)
            {
                if (CurrentLayerImages.ContainsKey(i))
                {
                    gr.DrawImage(CurrentLayerImages[i], new Rectangle(0, 0, frame.Width, frame.Height), new Rectangle(0, 0, frame.Width, frame.Height), GraphicsUnit.Pixel);
                    usedLayers++;
                }
                if (usedLayers >= CurrentLayerImages.Count)
                {
                    break;
                }
                i++;
            }
            return frame;
        }
    }
}