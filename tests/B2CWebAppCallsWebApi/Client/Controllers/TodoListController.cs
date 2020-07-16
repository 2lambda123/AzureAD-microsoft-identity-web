﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Threading.Tasks;
using TodoListClient.Services;
using TodoListService.Models;

namespace TodoListClient.Controllers
{
    public class TodoListController : Controller
    {
        private readonly ITodoListService _todoListService;
        private readonly ITokenAcquisition _tokenAcquisition;
        private const string Scope = "https://fabrikamb2c.onmicrosoft.com/tasks/read";
        private const string Susi = "b2c_1_susi";
        private const string EditProfile = "b2c_1_edit_profile";
        private const string Claims = "Claims";

        public TodoListController(ITodoListService todoListService, ITokenAcquisition tokenAcquisition)
        {
            _todoListService = todoListService;
            _tokenAcquisition = tokenAcquisition;
        }

        // GET: TodoList
        //[AuthorizeForScopes(ScopeKeySection = "TodoList:TodoListScope")]
        [AuthorizeForScopes(
            ScopeKeySection = "TodoList:TodoListScope", UserFlow = Susi)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
        public async Task<ActionResult> Index()
        {
            return View(await _todoListService.GetAsync(Susi));
        }

        [AuthorizeForScopes(Scopes = new string[] { Scope }, UserFlow = Susi)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
        public async Task<ActionResult> ClaimsSusi()
        {
            // We get a token, but we don't use it. It's only to trigger the user flow
            await _tokenAcquisition.GetAccessTokenForUserAsync(
                new string[] { Scope },
                userFlow: Susi);
            return View(Claims, null);
        }

        [AuthorizeForScopes(Scopes = new string[] { Scope }, UserFlow = EditProfile)] // Must be the same user flow as used in `GetAccessTokenForUserAsync()`
        public async Task<ActionResult> ClaimsEditProfile()
        {
            // We get a token, but we don't use it. It's only to trigger the user flow
            await _tokenAcquisition.GetAccessTokenForUserAsync(
                new string[] { Scope },
                userFlow: EditProfile);
            return View(Claims, null);
        }


        // GET: TodoList/Details/5
        public async Task<ActionResult> Details(int id)
        {
            return View(await _todoListService.GetAsync(id));
        }

        // GET: TodoList/Create
        public ActionResult Create()
        {
            Todo todo = new Todo() { Owner = HttpContext.User.Identity.Name };
            return View(todo);
        }

        // POST: TodoList/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Title,Owner")] Todo todo)
        {
            await _todoListService.AddAsync(todo);
            return RedirectToAction("Index");
        }

        // GET: TodoList/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            Todo todo = await _todoListService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        // POST: TodoList/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, [Bind("Id,Title,Owner")] Todo todo)
        {
            await _todoListService.EditAsync(todo);
            return RedirectToAction("Index");
        }

        // GET: TodoList/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            Todo todo = await _todoListService.GetAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return View(todo);
        }

        // POST: TodoList/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, [Bind("Id,Title,Owner")] Todo todo)
        {
            await _todoListService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
