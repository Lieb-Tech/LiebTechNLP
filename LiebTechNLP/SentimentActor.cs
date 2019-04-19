using Akka.Actor;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.neural.rnn;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.sentiment;
using edu.stanford.nlp.trees;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiebTechNLP
{
    class SentimentActor : ReceiveActor
    {
        public SentimentActor()
        {
            Receive<string>(i =>
            {
                var r = JsonConvert.DeserializeObject<SharedMessages.SentimentRequest>(i);
                if (r != null)
                {
                    var res = new SharedMessages.SentimentResponse()
                    {
                        id = r.id,
                        feed = r.feed,
                        section = r.section
                    };
                    foreach (var line in r.linesToProcess)
                    { 
                        var annotation = new Annotation(line);
                        Program.pipeline.annotate(annotation);

                        var sentences = annotation.get(typeof(CoreAnnotations.SentencesAnnotation)) as java.util.ArrayList;

                        // For each sentence in sentences, annotate and return the sentiment value
                        foreach (var s in sentences)
                        {
                            var sentence = s as Annotation;
                            var sentenceTree = sentence.get(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree)) as Tree;
                            var sentiment = RNNCoreAnnotations.getPredictedClass(sentenceTree);
                            var preds = RNNCoreAnnotations.getPredictions(sentenceTree);
                            var sent = "";

                            if (sentiment == 0)
                                sent = "Negative";
                            else if (sentiment == 1)
                                sent = "Somewhat negative";
                            else if (sentiment == 2)
                                sent = "Neutral";
                            else if (sentiment == 3)
                                sent = "Somewhat positive";
                            else if (sentiment == 4)
                                sent = "Positive";

                            res.results.Add(sentiment);
                        }
                    }

                    Sender.Tell("sent:" + JsonConvert.SerializeObject(res));
                }
            });
        }
    }
}
