using java.io;
using java.util;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.tagger.maxent;
using Console = System.Console;
using System;
using edu.stanford.nlp.ie.crf;
using System.IO;
using edu.stanford.nlp.pipeline;
using Akka.Actor;
using Akka.Remote;
using Akka.Configuration;

namespace LiebTechNLP
{
    class Program
    {
        internal static StanfordCoreNLP pipeline;
        internal static CRFClassifier classifier;
        
        static void setupPipeline()
        {
            DateTimeOffset started = DateTimeOffset.UtcNow;
            var classifiersDirectory = @"C:/temp/stanford-english-corenlp/edu/stanford/nlp/models/ner";

            // Loading 3 class classifier model
            classifier = CRFClassifier.getClassifierNoExceptions(classifiersDirectory + @"\english.all.3class.distsim.crf.ser.gz");


            // Path to the folder with models extracted from `stanford-corenlp-3.8.0-models.jar`            
            var jarRoot = @"c:/temp/stanford-english-corenlp/";
            // Annotation pipeline configuration
            var props = new Properties();
            props.setProperty("annotators", "tokenize, ssplit, pos, parse, sentiment");
            props.setProperty("ner.useSUTime", "0");
            
            var curDir = Environment.CurrentDirectory;
            Directory.SetCurrentDirectory(jarRoot);
            pipeline = new StanfordCoreNLP(props);
            Directory.SetCurrentDirectory(curDir);
            Console.WriteLine("finished up : " + (DateTimeOffset.UtcNow - started).TotalSeconds + " seconds");
        }

        static void Main(string[] args)
        {
            // gen();
            Console.WriteLine("starting up pipeline");
            setupPipeline();
            Console.WriteLine("starting up actors");

            var hocon = ConfigurationFactory.ParseString(@"akka {
    actor {
        provider = remote
    }

    remote {
        dot-netty.tcp {
            port = 8080
            hostname = localhost
        }
    }
}");

            using (var akka = ActorSystem.Create("nlp-system", hocon))
            {
                var actor = akka.ActorOf<NERActor>("ner");
                var actor2 = akka.ActorOf<SentimentActor>("sent");

                Console.WriteLine(actor.Path.ToString());
                Console.ReadLine();
            }
        }        
    }
}

