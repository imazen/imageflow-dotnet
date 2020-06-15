using System.Collections.Generic;
using System.Linq;
using Imageflow.Bindings;

// ReSharper disable CheckNamespace
namespace Imageflow.Fluent
{


    public class BuildJobResult
    {
        private Dictionary<int, BuildEncodeResult> _results;

        /// <summary>
        /// A collection of the encoded images produced by the job
        /// </summary>
        public IReadOnlyCollection<BuildEncodeResult> EncodeResults { get; private set; }

        /// <summary>
        /// The first encoded image produced by the job (with the lowest io_id)
        /// </summary>
        public BuildEncodeResult First => EncodeResults.FirstOrDefault();
        
        public BuildEncodeResult this[int ioId] => _results[ioId];
        
        /// <summary>
        /// Returns null if the encode result by the given io_id doesn't exist. 
        /// </summary>
        /// <param name="ioId"></param>
        /// <returns></returns>
        public BuildEncodeResult TryGet(int ioId) => _results.TryGetValue(ioId, out var result) ? result : null;

        internal static BuildJobResult From(IJsonResponseProvider response, Dictionary<int, IOutputDestination> outputs)
        {
            var v = response.DeserializeDynamic();
            if (v == null || v.success == null) throw new ImageflowAssertionFailed("BuildJobResult.From cannot parse response " + response.GetString());

            if (!(bool)v.success.Value) throw new ImageflowAssertionFailed("BuildJobResult.From cannot convert a failure");

            IEnumerable<dynamic> encodes = (v.data.job_result ?? v.data.build_result).encodes;

            var encodeResults = encodes.Select(er => new BuildEncodeResult
            {
                Width = er.w,
                Height = er.h,
                IoId = er.io_id,
                PreferredExtension = er.preferred_extension,
                PreferredMimeType = er.preferred_mime_type,
                Destination = outputs[(int)er.io_id.Value]
            }).OrderBy(er => er.IoId).ToList();

            var dict = encodeResults.ToDictionary(r => r.IoId);
   
            // There may be fewer reported outputs than registered ones - encoding is conditional on input, I think
            return new BuildJobResult {EncodeResults = encodeResults, _results = dict};
        }
    }
}

