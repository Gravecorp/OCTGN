﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Xml;

namespace Octgn.ProxyGenerator
{
    public class ProxyGenerator
    {
        private XmlDocument Doc { get; set; }
        private Dictionary<string, CardDefinition> cards = new Dictionary<string, CardDefinition>();
        private Dictionary<string, string> values = new Dictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public ProxyGenerator(string filePath)
        {
            Doc = new XmlDocument();
            Doc.Load(filePath);
            LoadCards();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        public ProxyGenerator(XmlDocument doc)
        {
            Doc = doc;
            LoadCards();
        }

        private void LoadCards()
        {
            XmlNodeList cardList = Doc.GetElementsByTagName("card");
            foreach (XmlNode card in cardList)
            {
                CardDefinition cardDef = CardDefinition.LoadCardDefinition(card);
                cards.Add(cardDef.id, cardDef);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void AddValue(string id, string value)
        {
            if (values.ContainsKey(id))
            {
                values.Remove(id);
            }
            values.Add(id, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearValues()
        {
            values.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Image GenerateProxy(string id)
        {
            CardDefinition cardDef = cards[id];
            Image ret = Image.FromFile(cardDef.filename);

            using (Graphics graphics = Graphics.FromImage(ret))
            {
                foreach (OverlayDefinition overlay in cardDef.Overlays)
                {
                    MergeOverlay(graphics, overlay);
                }

                foreach (SectionDefinition section in cardDef.Sections)
                {
                    if (values.ContainsKey(section.id))
                    {
                        WriteString(graphics, section, values[section.id]);
                    }
                }
            }

            return (ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="overlay"></param>
        public void MergeOverlay(Graphics graphics, OverlayDefinition overlay)
        {
            using (Image layer = Image.FromFile(overlay.filename))
            {
                graphics.DrawImageUnscaled(layer, overlay.location.ToPoint());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="section"></param>
        /// <param name="value"></param>
        public void WriteString(Graphics graphics, SectionDefinition section, string value)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            GraphicsPath path = null;
            if (section.block.width > 0 && section.block.height > 0)
            {
                path = GetTextPath(section.location.ToPoint(), section.text.size, value, section.block.ToSize());
            }
            else
            {
                path = GetTextPath(section.location.ToPoint(), section.text.size, value);
            }

            SolidBrush b = new SolidBrush(section.text.color);

            if (section.border.size > 0)
            {
                Pen p = new Pen(section.border.color, section.border.size);
                graphics.DrawPath(p, path);
                graphics.FillPath(b, path);
            }
            else
            {
                graphics.FillPath(b, path);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location"></param>
        /// <param name="size"></param>
        /// <param name="text"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private GraphicsPath GetTextPath(Point location, int size, string text, Size block)
        {
            GraphicsPath myPath = new GraphicsPath();
            FontFamily family = new FontFamily("Arial");
            int fontStyle = (int)FontStyle.Regular;
            StringFormat format = StringFormat.GenericDefault;
            Rectangle rect = new Rectangle(location, block);

            myPath.AddString(text,
                family,
                fontStyle,
                size,
                rect,
                format);

            return myPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location"></param>
        /// <param name="size"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public GraphicsPath GetTextPath(Point location, int size, string text)
        {
            GraphicsPath myPath = new GraphicsPath();
            FontFamily family = new FontFamily("Arial");
            int fontStyle = (int)FontStyle.Regular;
            StringFormat format = StringFormat.GenericDefault;

            myPath.AddString(text,
                family,
                fontStyle,
                size,
                location,
                format);

            return myPath;
        }


    }
}