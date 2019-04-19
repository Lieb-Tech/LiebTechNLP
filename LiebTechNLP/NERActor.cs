using Akka.Actor;
using Console = System.Console;
using edu.stanford.nlp.pipeline;
using java.io;
using java.util;
using edu.stanford.nlp.ie.crf;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LiebTechNLP
{
    class NERActor : ReceiveActor
    {
        public NERActor()
        {
            Receive<string>(r =>
            {
                var req = JsonConvert.DeserializeObject<SharedMessages.NERRequest>(r);
                var resp = new SharedMessages.NERResponse();
                foreach (var l in req.linesToProcess)
                {
                    var res = string.Format("<Results>{0}</Results>\n", Program.classifier.classifyWithInlineXML(l)).Replace("&", "(amp)");
                    XDocument xdoc = XDocument.Parse(res);
                    resp.results.Add(xdoc.Root.Elements().Select(z => z.Value).ToList());
                }
                resp.section = req.section;
                resp.id = req.id;
                resp.feed = req.feed;

                Sender.Tell("ner:" + JsonConvert.SerializeObject(resp));
            });

            Receive<int>(r =>
            { 
                var annotation = new Annotation(r.ToString());

                Program.pipeline.annotate(annotation);

                var NERs = annotation.get(typeof(edu.stanford.nlp.ling.CoreAnnotations.NamedEntityTagAnnotation));
                if (NERs != null)
                {
                    foreach (Annotation ner in NERs as ArrayList)
                    {
                        Console.WriteLine("NER: " + ner);
                    }
                }

                // these are all the sentences in this document
                // a CoreMap is essentially a Map that uses class objects as keys and has values with custom types
                var sentences = annotation.get(typeof(edu.stanford.nlp.ling.CoreAnnotations.SentencesAnnotation));
                if (sentences != null)
                {

                    foreach (Annotation sentence in sentences as ArrayList)
                    {
                        Console.WriteLine("Sent: " + sentence);
                    }
                }

                // Result - Pretty Print
                using (var stream = new ByteArrayOutputStream())
                {
                    var pw = new PrintWriter(stream);
                    Program.pipeline.prettyPrint(annotation, pw);
                    Console.WriteLine(stream.toString());
                    stream.close();
                }
            });
        }
    }
    
}
