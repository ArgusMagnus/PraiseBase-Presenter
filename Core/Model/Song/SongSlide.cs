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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace PraiseBase.Presenter.Model.Song
{
    /// <summary>
    /// A single slide with songtext and/or a background image
    /// </summary>
    [Serializable()]
    public class SongSlide : ICloneable, ISerializable
    {
        /// <summary>
        /// All text lines of this slide
        /// </summary>
        public List<string> Lines { get; set; }

        /// <summary>
        /// All translation lines of this slide
        /// </summary>
        public List<string> Translation { get; set; }

        /// <summary>
        /// Number of the slide background image
        /// </summary>
        public IBackground Background { get; set; }

        /// <summary>
        /// Size of the main text. This is used to maintain compatibility with PowerPraise
        /// </summary>
        public float TextSize { get; set; }

        /// <summary>
        /// Part name
        /// </summary>
        public string PartName { get; set; }

        /// <summary>
        /// Indicates wether this slide has a translation
        /// </summary>
        public bool Translated
        {
            get { return Translation.Count > 0 ? true : false; }
        }

        /// <summary>
        /// Gets or sets the text of this slide
        /// </summary>
        /// <param name="text"></param>
        public String Text
        {
            get
            {
                string txt = "";
                int i = 1;
                foreach (string str in Lines)
                {
                    txt += str;
                    if (i < Lines.Count)
                        txt += Environment.NewLine;
                    i++;
                }
                return txt;
            }
            set
            {
                Lines = new List<string>();
                string[] ln = value.Trim().Split(new[] { Environment.NewLine, "<br/>" }, StringSplitOptions.None);
                foreach (string sl in ln)
                {
                    Lines.Add(sl.Trim());
                }
            }
        }

        /// <summary>
        /// Gets or sets the translation of this slide
        /// </summary>
        /// <param name="text"></param>
        public String TranslationText
        {
            get
            {
                string txt = "";
                int i = 1;
                foreach (string str in Translation)
                {
                    txt += str;
                    if (i < Translation.Count)
                        txt += Environment.NewLine;
                    i++;
                }
                return txt;
            }
            set
            {
                Translation = new List<string>();
                string[] tr = value.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (string sl in tr)
                {
                    Translation.Add(sl.Trim());
                }
            }
        }

        /// <summary>
        /// The slide constructor
        /// </summary>
        public SongSlide()
        {
            Lines = new List<string>();
            Translation = new List<string>();
        }

        #region ICloneable Members

        /// <summary>
        /// Clones this slide
        /// </summary>
        /// <returns>A duplicate of this slide</returns>
        public object Clone()
        {
            var res = new SongSlide();
            res.Text = Text;
            res.TranslationText = TranslationText;
            res.PartName = PartName;
            res.TextSize = TextSize;
            res.Background = Background;
            return res;
        }

        #endregion ICloneable Members

        /// <summary>
        /// Returns the text on one line. This is mainly used
        /// in the song detail overview in the presenter.
        /// </summary>
        /// <returns>Text on one line</returns>
        public string GetOneLineText()
        {
            return Lines.Aggregate("", (current, str) => current + (str + " "));
        }

        /// <summary>
        /// Gets the translation on one line
        /// </summary>
        /// <returns></returns>
        public string GetOneLineTranslation()
        {
            return Translation.Aggregate("", (current, str) => current + (str + " "));
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int res =  + TextSize.GetHashCode();
            if (Background != null) 
            {
                res = res ^ Background.GetHashCode();
            }
            for (int i = 0; i < Lines.Count; i++)
            {
                res = res ^ Lines[i].GetHashCode();
            }
            for (int i = 0; i < Translation.Count; i++)
            {
                res = res ^ Translation[i].GetHashCode();
            }
            return res;
        }

        /// <summary>
        /// Gets the object data for serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Background", this.Background);
            info.AddValue("TextSize", this.TextSize);
            info.AddValue("PartName", this.PartName);
            info.AddValue("Lines", this.Lines);
            info.AddValue("Translation", this.Translation);
        }
    } ;
}