using System.Text.Json;
using System.Text.Json.Serialization;
using Tut.Common.Dto.MapDtos;
using Tut.Common.Models;
namespace Tut.Common.Business;

public class MockGeoService : IGeoService
{
   private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
   {
      PropertyNameCaseInsensitive = true,
      PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
      TypeInfoResolver = DirectionResponseJsonSerializationContext.Default,
      Converters = { new JsonStringEnumConverter() }
   };


    public Task<SearchLocationResultDto> SearchLocationByLocationName(string locationName, string googleKey, double latitude = double.MinValue,
        double longitude = double.MinValue)
    {
        SearchLocationResultDto result = new();
        result.Status = "OK";
        result.Results =
        [
            new SearchLocationItemDto()
            {
                Name = "مسجد التوحيد",
                FormattedAddress = "١٠ الوليد بن ثانيان - شيراتون",
                Geometry = new GeometryDto()
                {
                    Location = new GeometryLocationDto()
                    {
                        Lat = 30.0974,
                        Lng = 31.3736
                    }
                }
            },
            new SearchLocationItemDto()
            {
                Name = "قرطوفا للبرمجيات",
                FormattedAddress = "١٠ الوليد بن ثانيان - شيراتون المطار",
                Geometry = new GeometryDto()
                {
                    Location = new GeometryLocationDto()
                    {
                       Lat = 30.00585,
                       Lng = 31.22983
                    }
                }
            },

        ];
        return Task.FromResult(result);
    }

    public Task<SearchLocationResultDto> SearchByCoords(double latitude, double longitude, string googleKey)
    {
        SearchLocationResultDto result = new();
        result.Status = "OK";
        result.Results =
        [
            new SearchLocationItemDto()
            {
                Name = "مسجد التوحيد",
                FormattedAddress = "١٠ الوليد بن ثانيان - شيراتون",
                Geometry = new GeometryDto()
                {
                    Location = new GeometryLocationDto()
                    {
                        Lat = 30.0974,
                        Lng = 31.3736
                    }
                }
            },
        ];
        return Task.FromResult(result);
    }

    
    public Task<DirectionResponseDto?> GetRouteDataAsync(string apiKey, GLocation startLocation, GLocation endLocation)
    {
#pragma warning disable IL2026
       // JsonTypeInfo are stored in the Options object, so this is a false positive.
       DirectionResponseDto? dto = JsonSerializer.Deserialize<DirectionResponseDto>(
#pragma warning restore IL2026
          DirectionResponse, 
          Options);
       return Task.FromResult(dto);
    }

    public async Task<DirectionResponseDto?> GetRouteDataAsync(string apiKey, GLocation startLocation, GLocation endLocation, List<GLocation> waypoints)
    {
       return await GetRouteDataAsync(apiKey, startLocation, endLocation);
    }

    private const string DirectionResponse = """ 
                                                 {
                                                "geocoded_waypoints" : 
                                                [
                                                   {
                                                      "geocoder_status" : "OK",
                                                      "place_id" : "ChIJFzSDoiQXWBQRAEv_bQb1dNY",
                                                      "types" : 
                                                      [
                                                         "establishment",
                                                         "point_of_interest"
                                                      ]
                                                   },
                                                   {
                                                      "geocoder_status" : "OK",
                                                      "place_id" : "ChIJvzrNXRBHWBQRCrXWiB98Wok",
                                                      "types" : 
                                                      [
                                                         "premise",
                                                         "street_address"
                                                      ]
                                                   }
                                                ],
                                                "routes" : 
                                                [
                                                   {
                                                      "bounds" : 
                                                      {
                                                         "northeast" : 
                                                         {
                                                            "lat" : 30.1102705,
                                                            "lng" : 31.3735913
                                                         },
                                                         "southwest" : 
                                                         {
                                                            "lat" : 30.0058767,
                                                            "lng" : 31.2296601
                                                         }
                                                      },
                                                      "copyrights" : "Powered by Google, ©2025 Google",
                                                      "legs" : 
                                                      [
                                                         {
                                                            "distance" : 
                                                            {
                                                               "text" : "21.9 km",
                                                               "value" : 21944
                                                            },
                                                            "duration" : 
                                                            {
                                                               "text" : "31 mins",
                                                               "value" : 1876
                                                            },
                                                            "end_address" : "Old Roman Walls, Kom Ghorab, Old Cairo, Cairo Governorate 4244001, Egypt",
                                                            "end_location" : 
                                                            {
                                                               "lat" : 30.0058767,
                                                               "lng" : 31.2296601
                                                            },
                                                            "start_address" : "10 Prince Waleed Bin-Thinyan Al-Saud, Sheraton Al Matar, El Nozha, Cairo Governorate 4471315, Egypt",
                                                            "start_location" : 
                                                            {
                                                               "lat" : 30.0974703,
                                                               "lng" : 31.3735913
                                                            },
                                                            "steps" : 
                                                            [
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.1 km",
                                                                     "value" : 104
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 34
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.097371,
                                                                     "lng" : 31.37251539999999
                                                                  },
                                                                  "html_instructions" : "Head \u003cb\u003ewest\u003c/b\u003e on \u003cb\u003eFouad Thabit\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "eluvD}sn~DFjAJhC"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0974703,
                                                                     "lng" : 31.3735913
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "64 m",
                                                                     "value" : 64
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 23
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0967995,
                                                                     "lng" : 31.3725824
                                                                  },
                                                                  "html_instructions" : "Turn \u003cb\u003eleft\u003c/b\u003e toward \u003cb\u003eEl-Mosheer Ahmed Ismail St\u003c/b\u003e",
                                                                  "maneuver" : "turn-left",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "qkuvDgmn~DpBK"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.097371,
                                                                     "lng" : 31.37251539999999
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "74 m",
                                                                     "value" : 74
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 23
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0967497,
                                                                     "lng" : 31.3718126
                                                                  },
                                                                  "html_instructions" : "Turn \u003cb\u003eright\u003c/b\u003e at the 1st cross street toward \u003cb\u003eEl-Mosheer Ahmed Ismail St\u003c/b\u003e",
                                                                  "maneuver" : "turn-right",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "_huvDsmn~DBdADrA"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0967995,
                                                                     "lng" : 31.3725824
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "1.4 km",
                                                                     "value" : 1421
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "2 mins",
                                                                     "value" : 137
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.1061576,
                                                                     "lng" : 31.3622281
                                                                  },
                                                                  "html_instructions" : "Turn \u003cb\u003eright\u003c/b\u003e onto \u003cb\u003eEl-Mosheer Ahmed Ismail St\u003c/b\u003e",
                                                                  "maneuver" : "turn-right",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "uguvDyhn~DoCPi@JE?E@KBCDSNGFcAlAYZEDaAnAEDKLcApAmB`Cc@h@uCnDCDKJoCnDORKNKLi@r@]`@kEpFQTuEvFOP{BjCe@l@e@l@e@j@UPULk@\\KFC@E@SA"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0967497,
                                                                     "lng" : 31.3718126
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.5 km",
                                                                     "value" : 454
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 33
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.1082001,
                                                                     "lng" : 31.3663191
                                                                  },
                                                                  "html_instructions" : "Turn \u003cb\u003eright\u003c/b\u003e onto \u003cb\u003eEl-Orouba\u003c/b\u003e",
                                                                  "maneuver" : "turn-right",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "obwvD}ll~DACgAsCCKCGgEgKKSuAiDIUQi@"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.1061576,
                                                                     "lng" : 31.3622281
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.8 km",
                                                                     "value" : 754
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 73
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.10878649999999,
                                                                     "lng" : 31.3667779
                                                                  },
                                                                  "html_instructions" : "Keep \u003cb\u003eright\u003c/b\u003e to stay on \u003cb\u003eEl-Orouba\u003c/b\u003e",
                                                                  "maneuver" : "keep-right",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "gowvDofm~Ds@mBa@iA}D{JQg@K_@CMU]KKAAEACAE?C?C@E?C@C@CBC@C@CBCBABCDCH?F?D?DBFBPNNHJT^\\x@~D`K@@@@@@DBDB"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.1082001,
                                                                     "lng" : 31.3663191
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "2.4 km",
                                                                     "value" : 2373
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "2 mins",
                                                                     "value" : 143
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0975404,
                                                                     "lng" : 31.3458331
                                                                  },
                                                                  "html_instructions" : "Merge onto \u003cb\u003eEl-Orouba\u003c/b\u003e/\u003cwbr/\u003e\u003cb\u003eKobri Al Matar\u003c/b\u003e\u003cdiv style=\"font-size:0.9em\"\u003eContinue to follow El-Orouba\u003c/div\u003e",
                                                                  "maneuver" : "merge",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "}rwvDkim~Dl@fBn@dBnI`TtIdTZv@dAvB~@rB~AlDp@`BxEbK\\r@lJdSPZTj@rEdK^t@lA`CLV`BpDpAdC"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.10878649999999,
                                                                     "lng" : 31.3667779
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.9 km",
                                                                     "value" : 906
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 52
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0931293,
                                                                     "lng" : 31.3379361
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eEl-Galaa Bridge\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "sluvDmfi~D\\h@fAfBp@rAxChGDJbCfF~AjDP\\lAhCtBrELX^`AVr@x@lC"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0975404,
                                                                     "lng" : 31.3458331
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "1.2 km",
                                                                     "value" : 1229
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 87
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0870656,
                                                                     "lng" : 31.3272661
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eEl-Orouba\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "aqtvDcug~D|AfDbBjDzA~Cf@bAd@~@lBjElAvCHRrAvCp@vAvAvCJTXf@Vj@LV@D~@jB~@hBJPdBrDjBxD"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0931293,
                                                                     "lng" : 31.3379361
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.7 km",
                                                                     "value" : 702
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 45
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0836153,
                                                                     "lng" : 31.3211646
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eEl-Orouba Tunnel\u003c/b\u003e/\u003cwbr/\u003e\u003cb\u003eNafak Al Orouba\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "eksvDmre~Dh@~@t@tAl@jAz@hB`@x@lBfEl@nAn@rApBrEjAhCPb@h@pA"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0870656,
                                                                     "lng" : 31.3272661
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "4.0 km",
                                                                     "value" : 4040
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "4 mins",
                                                                     "value" : 263
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0642284,
                                                                     "lng" : 31.2865056
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eSalah Salem St\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "survDgld~DP\\hC~FJR\\x@Vj@r@jBt@vAp@fAZ`@z@tAn@r@lA|AJLn@|@zIjMfB~CHL`DvHRb@L\\xC`KXhA`DlMLf@@D~ArGZjAJf@n@bCBHlAnFRdA@@@F@BhCrKfA`EdAzEhAtEnB|H|@nCRl@Tn@Vj@JJFJFJJRNXNVRRZ\\V^^^?@h@h@rEnFHFLNLNh@j@v@t@RXLPJLHTjCbCXXZXZXr@p@rAtAjBlBDDfAlA@@b@l@vBbC"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0836153,
                                                                     "lng" : 31.3211646
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.8 km",
                                                                     "value" : 833
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 55
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.05805519999999,
                                                                     "lng" : 31.2816015
                                                                  },
                                                                  "html_instructions" : "Merge onto \u003cb\u003eSalah Salem St\u003c/b\u003e",
                                                                  "maneuver" : "merge",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "m|nvDus}}Dd@^xDlCdAt@b@Xr@j@xC|Bz@l@hFxDf@X~C`CfBnAj@f@v@p@"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0642284,
                                                                     "lng" : 31.2865056
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.5 km",
                                                                     "value" : 471
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 31
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0545769,
                                                                     "lng" : 31.2788177
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eNafak Ahmed Saeed\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "{umvD_u|}DvB|AZVzB`BB@NNDBTLTNdAt@HFz@l@^V^T@Bt@n@|ApA"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.05805519999999,
                                                                     "lng" : 31.2816015
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.3 km",
                                                                     "value" : 251
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 16
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.05272879999999,
                                                                     "lng" : 31.2773184
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eSalah Salem St\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "c`mvDsc|}DZVZXf@`@lAz@jBtAvAdA"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0545769,
                                                                     "lng" : 31.2788177
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "3.9 km",
                                                                     "value" : 3898
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "5 mins",
                                                                     "value" : 314
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0247273,
                                                                     "lng" : 31.259748
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eSalah Salem St\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "qtlvDgz{}DbBvAPN^ZrC`C^ZRNh@b@p@j@VThA|@bDjC~C~BvC|B~@r@tB`BFFbCpBx@r@v@r@zAnA`Av@TPRPNN~@v@VTVRhCtBrCfCZVhB~AzAtAZXbBzAjF`F~@dAhAbAb@^PJNHPDP@R?PCNEVQ`@]bBqAl@g@NKnA_ALKLMDCj@_@f@S@?^KJAZEb@Cn@E`DIhCKxH]j@Cb@AtCIT@TBXBhAd@v@j@x@r@l@n@^b@`@l@\\d@p@`Ab@p@jA~AlBhCnA`BV\\PL`@TRFVBVDHF`@Xf@b@z@`A\\d@V`@R`@JTJTHVJTJZNh@Lj@D\\BTLlA"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.05272879999999,
                                                                     "lng" : 31.2773184
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.6 km",
                                                                     "value" : 564
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 51
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0223227,
                                                                     "lng" : 31.2551285
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eKobri Al Sayeda Aesha\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "qegvDmlx}DLdCPvF@j@B`@@L@H@DBPBNDN@DBDFNFJHLJJTRVRdCjB\\Td@^bBvA^Z"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0247273,
                                                                     "lng" : 31.259748
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "2.5 km",
                                                                     "value" : 2519
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "4 mins",
                                                                     "value" : 236
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.016003,
                                                                     "lng" : 31.2339865
                                                                  },
                                                                  "html_instructions" : "Continue onto \u003cb\u003eSalah Salem St\u003c/b\u003e",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "ovfvDqow}DDFdF`EhE|Cb@VpDdCjAx@lAzA?@?B@B?BFHBBBBFDFBB?@@HH\\`@NRZXb@\\x@b@`CdA`@P\\N^N^LB@`Bl@FBVNF@RHHDJFBBDDDDBDLPFJHRDLBJBJ?F@F?H?FAn@EfAIdCGzAE|@Ef@EnAKfBM~BGnA_@tGS|DEt@IlAEn@El@G~@SdDEl@AVC^CVAb@Af@An@@b@Bb@Bd@Dh@Db@Ht@Hz@BXBV@F@F?F?F@F?D@F?B?B?D?BAB?B?BABAJSpA}AvL"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0223227,
                                                                     "lng" : 31.2551285
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "13 m",
                                                                     "value" : 13
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 12
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0161194,
                                                                     "lng" : 31.2339944
                                                                  },
                                                                  "html_instructions" : "Turn \u003cb\u003eright\u003c/b\u003e toward \u003cb\u003eSidey Hassan Al Anwar\u003c/b\u003e",
                                                                  "maneuver" : "turn-right",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "_oevDmks}DGAC?C?G@"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.016003,
                                                                     "lng" : 31.2339865
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "72 m",
                                                                     "value" : 72
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 22
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0159159,
                                                                     "lng" : 31.2335415
                                                                  },
                                                                  "html_instructions" : "Turn \u003cb\u003eleft\u003c/b\u003e toward \u003cb\u003eSidey Hassan Al Anwar\u003c/b\u003e",
                                                                  "maneuver" : "turn-left",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "woevDmks}DC@EBCBADCDAF?D?D?F@D@DBD@@JFJFPBD?B?"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0161194,
                                                                     "lng" : 31.2339944
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.9 km",
                                                                     "value" : 880
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "2 mins",
                                                                     "value" : 148
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0084476,
                                                                     "lng" : 31.2309704
                                                                  },
                                                                  "html_instructions" : "Keep \u003cb\u003eright\u003c/b\u003e to continue on \u003cb\u003eSidey Hassan Al Anwar\u003c/b\u003e, follow signs for \u003cb\u003eBasateen\u003c/b\u003e",
                                                                  "maneuver" : "keep-right",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "onevDshs}DNALALCLCTIJAFAB?PAP@P@LBB?LDNDr@PdAX`@J`@JzBf@LB~FlANBB@PFNFf@NB@v@\\p@TD@RHf@Tz@^fAb@l@Xd@NHDx@^f@TFBl@Xd@HF?BADA"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0159159,
                                                                     "lng" : 31.2335415
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               },
                                                               {
                                                                  "distance" : 
                                                                  {
                                                                     "text" : "0.3 km",
                                                                     "value" : 322
                                                                  },
                                                                  "duration" : 
                                                                  {
                                                                     "text" : "1 min",
                                                                     "value" : 78
                                                                  },
                                                                  "end_location" : 
                                                                  {
                                                                     "lat" : 30.0058767,
                                                                     "lng" : 31.2296601
                                                                  },
                                                                  "html_instructions" : "Turn \u003cb\u003eright\u003c/b\u003e onto \u003cb\u003eMari Gerges\u003c/b\u003e\u003cdiv style=\"font-size:0.9em\"\u003eDestination will be on the left\u003c/div\u003e",
                                                                  "maneuver" : "turn-right",
                                                                  "polyline" : 
                                                                  {
                                                                     "points" : "y_dvDqxr}DFJHHJFLFLHZRRPZXh@f@ZXNLFDBBNFPB\\Fd@F@?T@p@Hl@Fz@J"
                                                                  },
                                                                  "start_location" : 
                                                                  {
                                                                     "lat" : 30.0084476,
                                                                     "lng" : 31.2309704
                                                                  },
                                                                  "travel_mode" : "DRIVING"
                                                               }
                                                            ],
                                                            "traffic_speed_entry" : [],
                                                            "via_waypoint" : []
                                                         }
                                                      ],
                                                      "overview_polyline" : 
                                                      {
                                                         "points" : "eluvD}sn~DRtEpBKBdADrAoCPo@JQDWTeBpBoHfJuHpJqAdBqM`PwExFe@j@k@^w@d@IBSAACkA_DmHmQqBwFoEcLOm@a@i@GCQ?QFOJEHCPFd@XZr@xA`EbKBBJF|AlEdTfi@`BnD~C`Hp@`BxEbKjKxTf@fArFzLzAxC`BpDpAdCdBpCjE|IhCrFtHfPl@zApA`EdIvPrCjGvAjDhF|K`ApBpFzKtCxFbB`DxFzLpMxYt@dBr@jBt@vAlAhBz@tAn@r@xAjBjKhOpBlDtDzIL\\xC`KzDvOjClKlCdLXrAhCrKfA`EnCpLnB|H|@nCh@|AVj@JJNVZl@b@j@rA~A|FxGVVv@z@v@t@RXX^HTjCbCt@r@nAjAlGvGd@n@vBbCd@^~FbEvAdAtEjDpGrEfGpEbBxArGxE`Ap@jDbCtDzCv@p@tB|AbEzCtBfBfFhE|DbDbDjC~C~BvEpD|BhB|DdDjFlEzBnB`DhCnD~CdIjHjF`F~@dAlBbB`@Tb@Fd@Cf@WbEcDpB}Aj@_@f@S`@KzBQjHUdJa@xDKj@DXBhAd@v@j@x@r@lArA~@rAtArBxDhFfB~Br@b@j@J`@LhA|@xAfBj@bAVj@p@rBRhAPbBd@jMHn@Nj@NZTXpEhDnDzCdF`EhE|CtE|CjAx@lAzA?DHPVPD@f@j@j@l@|A`A`FvBlC`A|@`@ZVb@v@Ll@?hA]fJgBx\\}@vNEz@CvANvCd@~EBn@CNU|A}AvLGAG?KBORAZHRVNVBn@Gb@MRCx@@lFrAhKxBt@Tj@PhBr@|B`AdDrAvCrAl@HHCFJTPZPn@d@pBhBJH`@JzAPzC\\"
                                                      },
                                                      "summary" : "Salah Salem St",
                                                      "warnings" : [],
                                                      "waypoint_order" : []
                                                   }
                                                ],
                                                "status" : "OK"
                                             }
                                             """;
}