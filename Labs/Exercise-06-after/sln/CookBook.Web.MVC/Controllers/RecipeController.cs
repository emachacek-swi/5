﻿using CookBook.BL.Common.Services;
using CookBook.BL.Web.MVC.Api;
using CookBook.BL.Web.MVC.Facades;
using CookBook.Web.MVC.Models;
using CookBook.Web.MVC.ViewModels.Recipe;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace CookBook.Web.MVC.Controllers
{
    public class RecipeController : Controller
    {
        private readonly RecipeFacade _recipeFacade;
        private readonly IngredientFacade _ingredientFacade;
        private readonly ISerializerService serializerService;

        public RecipeController(
            RecipeFacade recipeFacade,
            IngredientFacade ingredientFacade,
            ISerializerService serializerService)
        {
            _recipeFacade = recipeFacade;
            _ingredientFacade = ingredientFacade;
            this.serializerService = serializerService;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var recipes = await _recipeFacade.GetAllAsync();
            var recipeListViewModel = new RecipeListViewModel
            {
                Recipes = recipes
            };
            return View(recipeListViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(Guid id)
        {
            var recipe = await _recipeFacade.GetByIdAsync(id);
            var recipeDetailViewModel = new RecipeDetailViewModel
            {
                RecipeDetail = recipe
            };
            return View(recipeDetailViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> New(RecipeNewViewModel recipeNewViewModel)
        {
            recipeNewViewModel.IngredientsAll ??= await _ingredientFacade.GetAllAsync();
            recipeNewViewModel.RecipeModel ??= new RecipeDetailModel
            {
                Ingredients = new List<RecipeListIngredientModel>()
            };
            recipeNewViewModel.RecipeModel.Ingredients ??= new List<RecipeListIngredientModel>();

            return View(recipeNewViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Insert(RecipeNewViewModel recipeNewViewModel)
        {
            var currentCulture = CultureInfo.CurrentCulture;

            if (TimeSpan.TryParse(recipeNewViewModel.DurationText, out TimeSpan duration))
            {
                recipeNewViewModel.RecipeModel.Duration = duration;
            }

            try
            {
                await _recipeFacade.InsertAsync(recipeNewViewModel.RecipeModel, culture: currentCulture.Name);
            }
            catch (ApiException e)
            {
                var validationErrorsModel = serializerService.Deserialize<ValidationErrorsModel<RecipeNewValidationErrorsModel>>(e.Response);

                foreach (var nameError in validationErrorsModel.Errors.Name)
                {
                    ModelState.AddModelError($"{nameof(recipeNewViewModel.RecipeModel)}.{nameof(recipeNewViewModel.RecipeModel.Name)}", nameError);
                }

                foreach (var descriptionError in validationErrorsModel.Errors.Description)
                {
                    ModelState.AddModelError($"{nameof(recipeNewViewModel.RecipeModel)}.{nameof(recipeNewViewModel.RecipeModel.Description)}", descriptionError);
                }

                foreach (var foodTypeError in validationErrorsModel.Errors.FoodType)
                {
                    ModelState.AddModelError($"{nameof(recipeNewViewModel.RecipeModel)}.{nameof(recipeNewViewModel.RecipeModel.FoodType)}", foodTypeError);
                }

                foreach (var durationError in validationErrorsModel.Errors.Duration)
                {
                    ModelState.AddModelError($"{nameof(recipeNewViewModel.RecipeModel)}.{nameof(recipeNewViewModel.RecipeModel.Duration)}", durationError);
                }

                foreach (var ingredientsError in validationErrorsModel.Errors.Ingredients)
                {
                    ModelState.AddModelError($"{nameof(recipeNewViewModel.RecipeModel)}.{nameof(recipeNewViewModel.RecipeModel.Ingredients)}", ingredientsError);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(nameof(New), recipeNewViewModel);
            }

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid recipeId)
        {
            await _recipeFacade.DeleteAsync(recipeId);
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> AddIngredient(RecipeNewViewModel recipeNewViewModel)
        {
            recipeNewViewModel.RecipeModel.Ingredients ??= new List<RecipeListIngredientModel>();
            recipeNewViewModel.RecipeModel.Ingredients.Add(new RecipeListIngredientModel());
            return View(nameof(New), recipeNewViewModel);
        }
    }
}