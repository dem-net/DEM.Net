// Attribution.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    // Source : Brutile
    // Inherits from Dictionary to allow Json serialization in SharpGlTF
    public class Attribution : Dictionary<string, string>
    {
        public Attribution() : this("Subject not set", null, null, null)
        {

        }
        public Attribution(string subject, string text = null, string url = null, string acknowledgement = null)
        {
            this.Subject = subject;
            this.Text = text;
            this.Url = url;
            this.Acknowledgement = acknowledgement;
        }

        public string Subject
        {
            get { return GetValue(nameof(Subject)); }
            set { base[nameof(Subject)] = value; }
        }
        public string Text
        {
            get { return GetValue(nameof(Text)); }
            set { base[nameof(Text)] = value;}
        }
        public string Url
        {
            get { return GetValue(nameof(Url)); }
            set { base[nameof(Url)] = value; }
        }
        public string Acknowledgement
        {
            get { return GetValue(nameof(Acknowledgement)); }
            set { base[nameof(Acknowledgement)] = value; }
        }

        private string GetValue(string key)
        {
            base.TryGetValue(key, out string value);
            return value;
        }
    }
}
