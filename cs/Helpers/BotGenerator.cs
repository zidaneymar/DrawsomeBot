using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using NoteTaker.HTTP;
using NoteTaker.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteTaker.Helpers
{
    public static class BotGenerator
    {
        public static async Task<ComposerBot> Parse(DrawsomePic pic)
        {
            var root = pic.Root;

            var steps = new List<ComposerStep>();

            while (root != null)
            {
                // if the root is if condition, then parse it by single step and break out.
                if (root.NextFalse != null)
                {
                    steps.Add(await BotGenerator.Parse(root));
                    break;
                }

                // else parse step by step iteratly
                steps.Add(await BotGenerator.Parse(root));
                if (root.Next != null)
                {
                    root = root.Next.Next;
                }
                else
                {
                    break;
                }
            }

            return new ComposerBot(steps);
        }

        public static async Task<List<ComposerStep>> ParseSteps(DrawsomePic pic)
        {
            var root = pic.Root;

            var steps = new List<ComposerStep>();

            while (root != null)
            {
                steps.Add(await BotGenerator.Parse(root));
                if (root.Next != null)
                {
                    root = root.Next.Next;
                }
                else
                {
                    break;
                }
            }

            return steps;
        }

        public static async Task<dynamic> Parse(DrawsomeShape shape)
        {
            dynamic step = null;
            var query = shape.Text;
            var luisResponse = await new LuisRecognizer("447f0c99416f450598e97ba887644f95", "e60298ef-464f-457b-9ea4-6f7791b350fd").GetPrediction(query);

            var content = GetContent(luisResponse);

            var shapeType = luisResponse.TopScoringIntent?.Intent;

            if (shape.NextFalse != null)
            {
                shapeType = nameof(IfCondition);
            }

            switch (shapeType)
            {
                case nameof(IfCondition):
                    step = new IfCondition(content ?? query);
                    if (shape.Next.Next != null)
                    {
                        (step as IfCondition).Steps = await ParseSteps(new DrawsomePic(shape.Next.Next));
                    }

                    if (shape.NextFalse.Next != null)
                    {
                        (step as IfCondition).ElseSteps = await ParseSteps(new DrawsomePic(shape.NextFalse.Next));
                    }
                    break;

                case nameof(SetProperty):
                    step = new SetProperty(content ?? query);
                    break;

                case nameof(HttpRequest):
                    step = new HttpRequest();
                    break;

                case nameof(SendActivity):
                default:
                    step = new SendActivity(content ?? query);
                    break;
            }

            return step;
        }

        public static string GetContent(LuisResult luisResponse)
        {
            return luisResponse.Entities.Where(item => item.Type.ToLower() == "content").ToList().FirstOrDefault()?.Entity;
        }
    }
}
