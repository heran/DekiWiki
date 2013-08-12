/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2011 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */


using NUnit.Framework;
using MindTouch.Deki;
using System;
using MindTouch.Dream;
using System.Text;

namespace MindTouch.Deki.Tests
{
    // <summary>
    //      Test the functions in MindTouch.Deki.DekiFont
    // </summary>
    [TestFixture]
    public class DekiFontTests
    {
        // <summary>
        // Test the constructor of DekiFont, feed it wrong and correct
        // inputs and make sure it responds appropriately
        // </summary>
        [Test]
        public void DekiFontConstructorTest()
        {
            DekiFont f;

            // Contruct with null constructor, should fail
            try{ f = new DekiFont(null); }
            catch (ArgumentException e)
            { Assert.IsTrue(e.GetType() == (new ArgumentNullException()).GetType() ); }

            // create small data, should fail
            byte[] data = { 0, 0, 0 };
            try { f = new DekiFont(data); }
            catch (ArgumentException e)
            { Assert.IsTrue("data buffer too small" == e.Message.ToString()); }

            // Load font file
            data = Plug.New("resource://mindtouch.deki/MindTouch.Deki.Resources.Arial.mtdf").Get().ToBytes();

            // Attempt to construct class, should pass
            f = new DekiFont(data);

            // Change to bad size, should fail
            byte[] new_data = new byte[data.Length - 1];
            for (int i = 0; i < data.Length - 1; i++)
            {
                new_data[i] = data[i];
            }

            try{ f = new DekiFont(new_data); }
            catch (ArgumentException e)
            { Assert.IsTrue("data size mismatch" == e.Message.ToString()); }

            // Change data to bad signature, should fail
            data[3] = 50;
            try { f = new DekiFont(data); }
            catch (ArgumentException e)
            { Assert.IsTrue("unknown data signature" == e.Message.ToString()); }
        }

        // <summary>
        // Test the Truncate() function by feeding it strings of different sizes.
        // </summary>
        [Test]
        public void TruncateTest()
        {
            // Load font file set the width of every character to 1 for easy testing
            byte[] data = Plug.New("resource://mindtouch.deki/MindTouch.Deki.Resources.Arial.mtdf").Get().ToBytes();
            int length = (int)data[4] + ((int)data[5] << 8) + ((int)data[6] << 16);
            for (int i = 0; i < length; i++)
            {
                data[8 + 3 * i + 2] = 1;
            }
            DekiFont font = new DekiFont(data);
            
            String testString = "HelloWorldThisIsMindTouchHowisLife?";
            String newString;

            // Test that Truncate does it's job properly for different length requirements
            for (int i = testString.Length; i > 0; i--)
            {
                newString = font.Truncate(testString, i);
                if (i == testString.Length)
                    Assert.AreEqual(testString, newString);
                else
                    Assert.AreEqual(new StringBuilder(testString.Substring(0,i)).Append("...").ToString(), newString);
            }

            // Test with int.MaxValue as max length, should return the original string
            newString = font.Truncate(testString, int.MaxValue);
            Assert.AreEqual(testString, newString); 

            // Test with 0 as max length, should throw error
            try { font.Truncate(testString, 0); }
            catch (ArgumentException e)
            { Assert.IsTrue(e.GetType() == (new ArgumentNullException()).GetType()); }

            // Change all letters to 0's to make sure that default width get used
            for (int i = 0; i < length; i++)
            {
                data[8 + 3 * i] = 0;
                data[8 + 3 * i + 1] = 0;
            }
            data[7] = 2; // change default width to 2

            font = new DekiFont(data);
            newString = font.Truncate(testString, 10);
            Assert.AreEqual(5+3, newString.Length);
        }

    }
}
