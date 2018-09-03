﻿using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace autotrade.CustomElements.Elements
{
    internal class ComboboxWithImage : ComboBox
    {
        private readonly Dictionary<int, Image> IMAGES_DICTIONARY = new Dictionary<int, Image>();

        public void AddItem(string text, Image image)
        {
            var index = Items.Add(text);
            IMAGES_DICTIONARY.Add(index, image);
        }

        public Image GetImageByIndex(int index)
        {
            return IMAGES_DICTIONARY[index];
        }
    }
}