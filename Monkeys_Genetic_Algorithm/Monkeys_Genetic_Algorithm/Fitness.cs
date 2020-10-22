namespace Fitness {
    using Carter;
    using Carter.ModelBinding;
    using Carter.Request;
    using Carter.Response;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static System.Console;
    
    public class HomeModule : CarterModule {
        static Dictionary<int, TargetRequest> Target = new Dictionary <int, TargetRequest> ();
        //we uses a dictionary to store the target JSON object, and uses that object's id as the key for the dictionary
        public HomeModule () {
            Post ("/target", async (req, res) => { //first remove a Target JSON object if that object has the same key as the one we're about to add
                                                    //then, add a Target JSON object to the dictionary using it's own id as the key.
                var t = await req.Bind<TargetRequest> ();
                WriteLine ($"..... POST /target receive {t}");
                
                if (Target.ContainsKey (t.id)) {
                    WriteLine ($"..... Remove {Target[t.id]}"); // Target[t.id] will returns an object with the key t.id
                    Target.Remove (t.id);
                }                
                
                Target.Add (t.id, t);
                WriteLine ($"..... Added {Target[t.id]}");
                return;
            });
            
            Post ("/assess", async (req, res) => {
                var areq = await req.Bind<AssessRequest> ();
                WriteLine ($"..... POST /assess receive {areq}");
                var genomes = areq.genomes; //a list of genomes.
                
                TargetRequest t;
                var target = "";
                if (Target.TryGetValue (areq.id, out t)) { //see if assess JSON object id matches a key in the Target dictionary
                    WriteLine ($"..... Target Found {t}");
                    target = t.target; //the target string
                } else {
                    WriteLine ($"..... Target Not Found - assumed empty");
                }
                
                var scores = genomes .Select ( g => { //scores will be a list
                    var len = Math.Min (target.Length, g.Length);
                    var h = Enumerable .Range (0, len)  
                        .Sum (i => Convert.ToInt32 (target[i] != g[i]));
                    h = h + Math.Max (target.Length, g.Length) - len;
                    return h;
                }) .ToList ();
                
                var min = scores .DefaultIfEmpty () .Min ();
                WriteLine ($"..... min {min}"); //the minimum score/best fitting of the genomes.
                
                WriteLine ($"..... send response {areq}");
                var ares = new AssessResponse { id = areq.id, scores = scores };
                await res.AsJson (ares); //send a response back to the Monkeys.
                return;
            });
        }
    }
    
    public class TargetRequest {
        public int id { get; set; }
        public bool parallel { get; set; }
        public string target { get; set; }
        public override string ToString () {
            return $"{{{id}, {parallel}, \"{target}\"}}";
        }  
    }    

    // public class TryRequest {
        // public int id { get; set; }
        // public bool parallel { get; set; }
        // public int monkeys { get; set; }
        // public int length { get; set; }
        // public int crossover { get; set; }
        // public int mutation { get; set; }
        // public int limit { get; set; }
        // public override string ToString () {
            // return $"{{{id}, {parallel}, {monkeys}, {length}, {crossover}, {mutation}, {limit}}}";
        // }
    // }
    
    // public class TopRequest {
        // public int id { get; set; }
        // public int loop { get; set; }
        // public int score { get; set; }
        // public string genome { get; set; }
        // public override string ToString () {
            // return $"{{{id}, {loop}, {score}, {genome}}}";
        // }  
    // }    
    
    public class AssessRequest {
        public int id { get; set; }
        public List<string> genomes { get; set; }
        public override string ToString () {
            return $"{{{id}, #{genomes.Count}}}";
        }  
    }
    
    public class AssessResponse {
        public int id { get; set; }
        public List<int> scores { get; set; }
        public override string ToString () {
            return $"{{{id}, #{scores.Count}}}";
        }  
    }
}

namespace Fitness {
    using Carter;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup {
        public void ConfigureServices (IServiceCollection services) {
            services.AddCarter ();
        }

        public void Configure (IApplicationBuilder app) {
            app.UseRouting ();
            app.UseEndpoints( builder => builder.MapCarter ());
        }
    }
}

namespace Fitness {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program {
        public static void Main (string[] args) {
//          var host = Host.CreateDefaultBuilder (args)
//              .ConfigureWebHostDefaults (webBuilder => webBuilder.UseStartup<Startup>())

            var urls = new[] {"http://localhost:8091"};
            
            var host = Host.CreateDefaultBuilder (args)
            
                .ConfigureLogging (logging => {
                    logging
                        .ClearProviders ()
                        .AddConsole ()
                        .AddFilter (level => level >= LogLevel.Warning);
                })
                
                .ConfigureWebHostDefaults (webBuilder => {
                    webBuilder.UseStartup<Startup> ();
                    webBuilder.UseUrls (urls);  // !!!
                })
                
                .Build ();
            
            System.Console.WriteLine ($"..... starting on {string.Join (", ", urls)}");            
            host.Run ();
        }
    }
}

