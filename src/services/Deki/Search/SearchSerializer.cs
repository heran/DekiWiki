/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MindTouch.Deki.Data;
using MindTouch.IO;

namespace MindTouch.Deki.Search {
    public class SearchSerializer : ISerializer {

        //--- Methods ---
        public T Deserialize<T>(Stream stream) {
            if(typeof(T) == typeof(SearchResultDetail)) {
                return (T)DeserializeSearchResultDetail(stream);
            }
            if(typeof(T) == typeof(SearchResult)) {
                return (T)DeserializeSearchResult(stream);
            }
            throw new ArgumentException("This serializer only suports 'SearchResult' & 'SearchResultDetail'");
        }

        public void Serialize<T>(Stream stream, T data) {
            if(typeof(T) == typeof(SearchResultDetail)) {
                SerializeSearchResultDetail(data as SearchResultDetail, stream);
            } else if(typeof(T) == typeof(SearchResult)) {
                SerializeSearchResult(data as SearchResult, stream);
            } else {
                throw new ArgumentException("This serializer only suports 'SearchResult' & 'SearchResultDetail'");
            }
        }

        private void SerializeSearchResult(SearchResult data, Stream stream) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            SerializeString(stream, data.ExecutedQuery);
            stream.Write(BitConverter.GetBytes(data.Count));
            foreach(var item in data) {
                stream.Write(BitConverter.GetBytes(item.TypeId));
                stream.WriteByte((byte)item.Type);
                stream.Write(BitConverter.GetBytes(item.Rank));
                stream.Write(BitConverter.GetBytes(item.Modified.ToEpoch()));
                var stringBytes = Encoding.UTF8.GetBytes(item.Title);
                stream.Write(BitConverter.GetBytes(stringBytes.Length));
                stream.Write(stringBytes);
            }
        }

        private void SerializeSearchResultDetail(SearchResultDetail data, Stream stream) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            var fields = data.ToArray();
            stream.Write(BitConverter.GetBytes(fields.Length));
            foreach(var kvp in data) {
                SerializeString(stream, kvp.Key);
                SerializeString(stream, kvp.Value);
            }
        }

        private object DeserializeSearchResult(Stream stream) {
            var items = new List<SearchResultItem>();
            var parsedQuery = DeserializeString(stream);
            var count = BitConverter.ToInt32(stream.ReadBytes(sizeof(Int32)), 0);
            const int recordSize = sizeof(UInt32) + 1 + sizeof(double) + sizeof(UInt32) + sizeof(Int32);
            for(var i = 0; i < count; i++) {
                var bytes = stream.ReadBytes(recordSize);
                var index = 0;
                var typeId = BitConverter.ToUInt32(bytes, index);
                index += sizeof(UInt32);
                var type = (SearchResultType)bytes[index];
                index++;
                var rank = BitConverter.ToDouble(bytes, index);
                index += sizeof(double);
                var modified = DateTimeUtil.FromEpoch(BitConverter.ToUInt32(bytes, index));
                index += sizeof(UInt32);
                var stringByteCount = BitConverter.ToInt32(bytes, index);
                var title = Encoding.UTF8.GetString(stream.ReadBytes(stringByteCount), 0, stringByteCount);
                items.Add(new SearchResultItem(typeId, type, title, rank, modified));
            }
            return new SearchResult(parsedQuery, items);
        }

        private object DeserializeSearchResultDetail(Stream stream) {
            var count = BitConverter.ToInt32(stream.ReadBytes(sizeof(UInt32)), 0);
            var fields = new List<KeyValuePair<string, string>>();
            for(var i = 0; i < count; i++) {
                var key = DeserializeString(stream);
                var value = DeserializeString(stream);
                fields.Add(new KeyValuePair<string, string>(key, value));
            }
            return new SearchResultDetail(fields);
        }

        private static string DeserializeString(Stream stream) {
            var stringByteCount = BitConverter.ToInt32(stream.ReadBytes(sizeof(Int32)), 0);
            if(stringByteCount == -1) {
                return null;
            }
            if(stringByteCount == 0) {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(stream.ReadBytes(stringByteCount), 0, stringByteCount);
        }

        private static void SerializeString(Stream stream, string value) {
            if(value == null) {
                stream.Write(BitConverter.GetBytes(-1));
                return;
            }
            if(value.Length == 0) {
                stream.Write(BitConverter.GetBytes(0));
                return;
            }
            var stringBytes = Encoding.UTF8.GetBytes(value);
            stream.Write(BitConverter.GetBytes(stringBytes.Length));
            stream.Write(stringBytes);
        }
    }
}
