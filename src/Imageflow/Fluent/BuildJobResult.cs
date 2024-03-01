using Imageflow.Bindings;

// ReSharper disable CheckNamespace
namespace Imageflow.Fluent
{


    public class BuildJobResult
    {

        private BuildJobResult(IReadOnlyCollection<BuildDecodeResult> decodeResults, 
            IReadOnlyCollection<BuildEncodeResult> encodeResults, 
            PerformanceDetails performanceDetails)
        {
            _encodeResults =  encodeResults.ToDictionary(r => r.IoId);
            DecodeResults = decodeResults;
            EncodeResults = encodeResults;
            PerformanceDetails = performanceDetails;
        }

        private readonly Dictionary<int, BuildEncodeResult> _encodeResults;

        /// <summary>
        /// A collection of the decoded image details produced by the job
        /// </summary>
        public IReadOnlyCollection<BuildDecodeResult> DecodeResults { get; private set; }
        
        /// <summary>
        /// A collection of the encoded images produced by the job
        /// </summary>
        public IReadOnlyCollection<BuildEncodeResult> EncodeResults { get; private set; }
        
        /// <summary>
        /// Details about the runtime performance of the job
        /// </summary>
        public PerformanceDetails PerformanceDetails { get; private set; }

        /// <summary>
        /// The first encoded image produced by the job (with the lowest io_id)
        /// </summary>
        public BuildEncodeResult? First => EncodeResults.FirstOrDefault();
        
        public BuildEncodeResult this[int ioId] => _encodeResults[ioId];
        
        /// <summary>
        /// Returns null if the encode result by the given io_id doesn't exist. 
        /// </summary>
        /// <param name="ioId"></param>
        /// <returns></returns>
        public BuildEncodeResult? TryGet(int ioId) => _encodeResults.TryGetValue(ioId, out var result) ? result : null;

        internal static BuildJobResult From(IJsonResponse response, Dictionary<int, IOutputDestination> outputs)
        {
            var v = response.Parse();
            if (v == null) throw new ImageflowAssertionFailed("BuildJobResult.From cannot parse response " + response.CopyString());
            bool? success = v.AsObject().TryGetPropertyValue("success", out var successValue) 
                ? successValue?.GetValue<bool>() 
                : null;
            switch (success)
            {
                case false:
                    throw new ImageflowAssertionFailed("BuildJobResult.From cannot convert a failure: " + response.CopyString());
                case null:
                    throw new ImageflowAssertionFailed("BuildJobResult.From cannot parse response " + response.CopyString());
            }
            
            var data = v.AsObject().TryGetPropertyValue("data", out var dataValue) 
                ? dataValue
                : null;
            if (data == null) throw new ImageflowAssertionFailed("BuildJobResult.From cannot parse response ('data' missing) " + response.CopyString());


            // IEnumerable<dynamic> encodes = (v.data.job_result ?? v.data.build_result).encodes;
            //
            // var encodeResults = encodes.Select(er => new BuildEncodeResult
            // {
            //     Width = er.w,
            //     Height = er.h,
            //     IoId = er.io_id,
            //     PreferredExtension = er.preferred_extension,
            //     PreferredMimeType = er.preferred_mime_type,
            //     Destination = outputs[(int)er.io_id.Value]
            // }).OrderBy(er => er.IoId).ToList();
            //
            // IEnumerable<dynamic> decodes = (v.data.job_result ?? v.data.build_result).decodes ?? Enumerable.Empty<dynamic>();
            //
            // var decodeResults = decodes.Select(er => new BuildDecodeResult
            // {
            //     Width = er.w,
            //     Height = er.h,
            //     IoId = er.io_id,
            //     PreferredExtension = er.preferred_extension,
            //     PreferredMimeType = er.preferred_mime_type,
            // }).OrderBy(er => er.IoId).ToList();
            
            var jobResult = data.AsObject().TryGetPropertyValue("job_result", out var jobResultValue) 
                ? jobResultValue
                : null;
            
            if (jobResult == null) data.AsObject().TryGetPropertyValue("build_result", out jobResultValue);
            if (jobResult == null) throw new ImageflowAssertionFailed("BuildJobResult.From cannot parse response (missing job_result or build_result) " + response.CopyString());
            
            var encodeResults = new List<BuildEncodeResult>();
            if (!jobResult.AsObject().TryGetPropertyValue("encodes", out var encodeArray))
            {
                // This is unusual
                throw new ImageflowAssertionFailed("encodes = null");
            }

            if (!jobResult.AsObject().TryGetPropertyValue("decodes", out var decodeArray))
            {
                throw new ImageflowAssertionFailed("decodes = null");
            }
            var requiredMessage = "BuildJobResult.From cannot parse response (missing required properties io_id, w, h, preferred_extension, or preferred_mime_type) " + response.CopyString();    

            // Parse from JsonNode
            foreach (var encode in encodeArray?.AsArray() ?? [])
            {
                if (encode == null) continue;
                // ioId, w, h, preferred_extension, preferred_mime_type are required
                if (!encode.AsObject().TryGetPropertyValue("io_id", out var ioIdValue)
                    || !encode.AsObject().TryGetPropertyValue("w", out var wValue)
                    || !encode.AsObject().TryGetPropertyValue("h", out var hValue)
                    || !encode.AsObject().TryGetPropertyValue("preferred_extension", out var preferredExtensionValue)
                    || !encode.AsObject().TryGetPropertyValue("preferred_mime_type", out var preferredMimeTypeValue))
                {
                    throw new ImageflowAssertionFailed(requiredMessage);
                }
                var ioId = ioIdValue?.GetValue<int>() ?? throw new ImageflowAssertionFailed(requiredMessage);
                encodeResults.Add(new BuildEncodeResult
                {
                    IoId = ioId,
                    Width = wValue?.GetValue<int>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    Height = hValue?.GetValue<int>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    PreferredExtension = preferredExtensionValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    PreferredMimeType = preferredMimeTypeValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    Destination = outputs[ioId]
                });
            }
            
            // Parse from JsonNode
            var decodeResults = new List<BuildDecodeResult>();
            foreach (var decode in (decodeArray?.AsArray() ?? []))
            {
                if (decode == null) continue;
                // ioId, w, h, preferred_extension, preferred_mime_type are required
                if (!decode.AsObject().TryGetPropertyValue("io_id", out var ioIdValue)
                    || !decode.AsObject().TryGetPropertyValue("w", out var wValue)
                    || !decode.AsObject().TryGetPropertyValue("h", out var hValue)
                    || !decode.AsObject().TryGetPropertyValue("preferred_extension", out var preferredExtensionValue)
                    || !decode.AsObject().TryGetPropertyValue("preferred_mime_type", out var preferredMimeTypeValue))
                {
                    throw new ImageflowAssertionFailed(requiredMessage);
                }
                
                decodeResults.Add(new BuildDecodeResult
                {
                    IoId = ioIdValue?.GetValue<int>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    Width = wValue?.GetValue<int>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    Height = hValue?.GetValue<int>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    PreferredExtension = preferredExtensionValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                    PreferredMimeType = preferredMimeTypeValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(requiredMessage),
                });
            }

            
            
            var perfDetails = new PerformanceDetails(jobResult.AsObject().TryGetPropertyValue("performance", out var perfValue) ? perfValue : null);

            // There may be fewer reported outputs than registered ones - encoding is conditional on input, I think
            return new BuildJobResult(decodeResults.OrderBy(er => er.IoId).ToList(), 
                encodeResults.OrderBy(er => er.IoId).ToList(), perfDetails);
        }
        
        
    }

   
}

