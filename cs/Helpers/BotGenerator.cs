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

            var allSteps = new List<ComposerStep>();

            var rootSteps = await BuildFromStepUntil(pic, root, null, allSteps);
            var bot = new ComposerBot(rootSteps);
            bot.AllSteps = allSteps;
            return bot;
        }

        public static async Task<ComposerStep> BuildComposerStepFromShape(DrawsomeShape shape)
        {
            var query = shape.Text;
            var luisResponse = await new LuisRecognizer("447f0c99416f450598e97ba887644f95", "e60298ef-464f-457b-9ea4-6f7791b350fd").GetPrediction(query);

            var content = GetContent(luisResponse);

            var shapeType = luisResponse.TopScoringIntent?.Intent;

            switch (shapeType)
            {
                case nameof(IfCondition):
                    var ifStep = new IfCondition(content, shape);
                    return ifStep;

                case nameof(SetProperty):
                    var setStep = new SetProperty(content ?? query, shape);
                    return setStep;

                case nameof(TextInput):
                    var textStep = new TextInput(content ?? query, shape);
                    return textStep;

                case nameof(HttpRequest):
                    var httpStep = new HttpRequest(content ?? query, shape);
                    return httpStep;

                case nameof(SendActivity):
                default:
                    var sendStep = new SendActivity(content ?? query, shape);
                    return sendStep;
            }

        }

        // return a list of step from cur root
        public static async Task<List<ComposerStep>> BuildFromStepUntil(DrawsomePic pic, DrawsomeObj root, DrawsomeObj target, List<ComposerStep> allSteps)
        {
            var steps = new List<ComposerStep>();
            
            if (root == target)
            {
                return steps;
            }

            // only one next
            if (root.Next.Count != 2)
            {
                if (root is DrawsomeShape)
                {
                    var step = await BuildComposerStepFromShape(root as DrawsomeShape);
                    steps.Add(step);
                    allSteps.Add(step);
                    steps.AddRange(await BuildFromStepUntil(pic, root.Next.FirstOrDefault(), target, allSteps));
                }
                else if (root is DrawsomeLine)
                {
                    steps.AddRange(await BuildFromStepUntil(pic, root.Next.FirstOrDefault(), target, allSteps));
                }
            }

            if (root.Next.Count == 2)
            {
                if (root is DrawsomeShape)
                {
                    var firstCommon = NearestObj(pic, root);
                    var step = new IfCondition((root as DrawsomeShape).Text, root as DrawsomeShape);
                    allSteps.Add(step);
                    step.Steps = await BuildFromStepUntil(pic, root.Next.FirstOrDefault(), firstCommon, allSteps);
                    step.ElseSteps = await BuildFromStepUntil(pic, root.Next.LastOrDefault(), firstCommon, allSteps);
                    steps.Add(step);
                    steps.AddRange(await BuildFromStepUntil(pic, firstCommon, target, allSteps));
                }
            }

            return steps;
        }


        private static DrawsomeObj NearestObj(DrawsomePic pic, DrawsomeObj root)
        {
            var trueList = new List<DrawsomeObj>();

            var trueRoot = root.Next?[0];

            while (trueRoot != null)
            {
                trueList.Add(trueRoot);
                trueRoot = trueRoot.Next.FirstOrDefault();
            }
            var falseList = new List<DrawsomeObj>();

            var falseRoot = root.Next?[1];

            while (falseRoot != null)
            {
                falseList.Add(falseRoot);
                falseRoot = falseRoot.Next.FirstOrDefault();
            }

            var commonList = trueList.Intersect(falseList);

            var firstCommon = commonList?.FirstOrDefault();

            return firstCommon;
            
        }

        public static string GetContent(LuisResult luisResponse)
        {
            return luisResponse.Entities.Where(item => item.Type.ToLower() == "content").ToList().FirstOrDefault()?.Entity;
        }
    }
}
