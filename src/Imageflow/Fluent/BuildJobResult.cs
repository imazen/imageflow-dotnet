using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Imageflow.Bindings;
using Newtonsoft.Json.Serialization;

// ReSharper disable CheckNamespace
namespace Imageflow.Fluent
{
    public class BuildEncodeResult
    {
        public string PreferredMimeType { get; internal set; }
        public string PreferredExtension { get;internal set; }
        public int IoId { get; internal set;}
        public int Width { get; internal set;}
        public int Height { get; internal set;}
        
        public IOutputDestination Destination { get; internal set;}
        
        /// <summary>
        /// If this Destination is a BytesDestination, returns the ArraySegment - otherwis enull
        /// Returns the byte segment for the given output ID (if that output is a BytesDestination)
        /// </summary>
        public ArraySegment<byte>? TryGetBytes() => (Destination is BytesDestination d) ? d.GetBytes() : (ArraySegment<byte>?)null;
    }
    
    //    TODO: Currently we only support "Elsewhere", because we don't allow requesting any of these (yet)
//#[derive(Serialize, Deserialize, Clone, PartialEq, Debug)]
//    pub enum ResultBytes {
//#[serde(rename="base_64")]
//        Base64(String),
//#[serde(rename="byte_array")]
//        ByteArray(Vec<u8>),
//#[serde(rename="physical_file")]
//        PhysicalFile(String),
//#[serde(rename="elsewhere")]
//        Elsewhere,
//    }
//#[derive(Serialize, Deserialize, Clone, PartialEq, Debug)]
//    pub struct EncodeResult {
//        pub preferred_mime_type: String,
//        pub preferred_extension: String,
//
//        pub io_id: i32,
//        pub w: i32,
//        pub h: i32,
//
//        pub bytes: ResultBytes,
//    }
    public class BuildJobResult
    {
        private Dictionary<int, BuildEncodeResult> _results;

        public IEnumerable<BuildEncodeResult> EncodeResults { get; private set; }

        public BuildEncodeResult First => EncodeResults.FirstOrDefault();

        public BuildEncodeResult this[int ioId] => _results[ioId];
         
        public BuildEncodeResult TryGet(int ioId) => _results.TryGetValue(ioId, out var result) ? result : null;

        internal static BuildJobResult From(JsonResponse response, Dictionary<int, IOutputDestination> outputs)
        {
            var v = response.DeserializeDynamic();
            if (!(bool)v.success.Value) throw new ImageflowAssertionFailed("BuildJobResult.From cannot convert a failure");

            IEnumerable<dynamic> encodes = (v.data.job_result ?? v.data.build_result).encodes;

            var encodeResults = encodes.Select((er) => new BuildEncodeResult
            {
                Width = er.w,
                Height = er.h,
                IoId = er.io_id,
                PreferredExtension = er.preferred_extension,
                PreferredMimeType = er.preferred_mime_type,
                Destination = outputs[(int)er.io_id.Value]
            }).OrderBy(er => er.IoId).ToList();

            var dict = new Dictionary<int, BuildEncodeResult>();
            foreach (var r in encodeResults)
            {
                dict.Add(r.IoId, r);
            }
            // There may be fewer reported outputs than registered ones - encoding is conditional on input, I think
            return new BuildJobResult {EncodeResults = encodeResults, _results = dict};
        }
    }
}

