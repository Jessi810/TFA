﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TFA.Models;

namespace TFA.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private SecLogContext secLog = new SecLogContext();

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // POST: /Account/Clear2FARememberedBrowser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Clear2FARememberedBrowser()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
            return RedirectToAction("Index", "Manage", new { Message = ManageMessageId.Clear2FASuccess });
        }

        //
        // GET: /Manage/ChangeEmail
        public async Task<ActionResult> ChangeEmail()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }

            return View();
        }

        //
        // POST: /Manage/ChangeEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user != null)
            {
                user.Email = model.Email;
                await UserManager.UpdateAsync(user);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangeEmailSuccess });
            }
            else
            {
                return View("Error");
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : message == ManageMessageId.AddImagePasswordSuccess ? "Your image password was added."
                : message == ManageMessageId.ChangeImagePasswordSuccess ? "Your image password has been changed."
                : message == ManageMessageId.RemoveImagePasswordSuccess ? "Your image password was removed."
                : message == ManageMessageId.ChangeEmailSuccess ? "Your email has been changed."
                : message == ManageMessageId.ImageLoginSuccess ? "Login successful."
                : message == ManageMessageId.ChangeNotificationSuccess ? "Notification settings has been changed."
                : message == ManageMessageId.SentConfirmEmail ? "An email verification link has been sent to your email."
                : message == ManageMessageId.Clear2FASuccess ? "Remembered browser has been cleared. When you login again, you'll be asked again for 2nd and 3rd authentication credentials."
                : "";

            var userId = User.Identity.GetUserId();
            ApplicationUser user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            DateTime serverDate = DateTime.Now;
            var diff = user.PasswordResetDate.Subtract(serverDate);
            if (diff.Days > 0 && diff.Days < 15)
            {

            }
            else if (diff.Days < 0)
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

                return RedirectToAction("PasswordExpired", "Account", new { Email = user.Email });
            }

            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                DaysToResetPassword = diff.Days,
                ThreeFactorEnabled = user.ThreeFactorEnabled,
                ImagePasswordSet = user.SerialHash == null ? false : true,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed
            };
            return View(model);
        }

        public void SendNotifications(ApplicationUser user, string lockoutType, NotificationType nt)
        {
            try
            {
                var sl = new SecLog
                {
                    Type = String.IsNullOrEmpty(lockoutType) ? "User Account Lockout" : lockoutType,
                    Date = DateTime.Now,
                    UserName = user.UserName
                };
                secLog.SecLogs.Add(sl);
                secLog.SaveChanges();

                string date = DateTime.Now.ToLongDateString();
                switch (nt)
                {
                    case NotificationType.AccountLockout:
                        if (user.AccountLockoutSMS) SendSmsNotification(user, "Account Lockout", "Your account " + user.Email + " has been locked out because of failed login attempt.");
                        if (user.AccountLockoutEmail) SendEmailNotification(user, "Account Lockout", "Your account " + user.Email + " has been locked out because of failed login attempt.");
                        break;
                    case NotificationType.ChangePassword:
                        if (user.ChangePasswordSMS) SendSmsNotification(user, "Password Changed", "Your password has been changed on " + date);
                        if (user.ChangePasswordEmail) SendEmailNotification(user, "Password Changed", "Your password has been changed on " + date);
                        break;
                    case NotificationType.ChangePhoneNumber:
                        if (user.ChangePasswordSMS) SendSmsNotification(user, "Phone Number Changed", "Your phone number has been changed on " + date);
                        if (user.ChangePasswordEmail) SendEmailNotification(user, "Phone Number Changed", "Your phone number has been changed on " + date);
                        break;
                }

                //if (UserManager.SmsService != null)
                //{
                //}
            }
            catch (Exception e)
            {
                View("Error");
            }
        }

        public bool SendSmsNotification(ApplicationUser user, string subject, string msg)
        {
            try
            {
                var message = new IdentityMessage
                {
                    Destination = user.PhoneNumber,
                    Body = String.IsNullOrEmpty(msg) ? ("Your account " + user.Email + " has been locked out because of failed login attempt.") : msg,
                    Subject = String.IsNullOrEmpty(subject) ? "Three-Factor Authentication" : subject
                };
                UserManager.SmsService.Send(message);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool SendEmailNotification(ApplicationUser user, string subject, string msg)
        {
            try
            {
                UserManager.SendEmail(
                    user.Id,
                    String.IsNullOrEmpty(subject) ? "Three-Factore Authentication" : msg,
                    String.IsNullOrEmpty(msg) ? "Your account has been lockout because of failed login attempt." : msg
                    );

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public enum NotificationType
        {
            ChangePassword,
            ChangePhoneNumber,
            AccountLockout
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/NotificationSettings
        public async Task<ActionResult> NotificationSettings()
        {
            NotificationSettings ns;
            ApplicationUser user;

            try
            {
                ns = new NotificationSettings();
                user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            }
            catch (Exception e)
            {
                return View("Error");
            }

            ns.ChangePasswordEmail = user.ChangePasswordEmail;
            ns.ChangePasswordSMS = user.ChangePasswordSMS;
            ns.ChangePhoneNumberEmail = user.ChangePhoneNumberEmail;
            ns.ChangePhoneNumberSMS = user.ChangePhoneNumberSMS;
            ns.AccountLockoutEmail = user.AccountLockoutEmail;
            ns.AccountLockoutSMS = user.AccountLockoutSMS;
            return View(ns);
        }

        //
        // POST: /Manage/NotificationSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NotificationSettings(NotificationSettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            user.ChangePasswordEmail = model.ChangePasswordEmail;
            user.ChangePasswordSMS = model.ChangePasswordSMS;
            user.ChangePhoneNumberEmail = model.ChangePhoneNumberEmail;
            user.ChangePhoneNumberSMS = user.ChangePhoneNumberSMS;
            user.AccountLockoutEmail = model.AccountLockoutEmail;
            user.AccountLockoutSMS = model.AccountLockoutSMS;

            try
            {
                await UserManager.UpdateAsync(user);
            }
            catch (Exception e)
            {
                return View("Error");
            }

            return RedirectToAction("Index", "Manage", new { Message = ManageMessageId.ChangeNotificationSuccess });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/EnableThreeFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableThreeFactorAuthentication()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                user.ThreeFactorEnabled = true;
                await UserManager.UpdateAsync(user);
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableThreeFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableThreeFactorAuthentication()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                user.ThreeFactorEnabled = false;
                await UserManager.UpdateAsync(user);
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                SendNotifications(user, "", NotificationType.ChangePhoneNumber);
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCurrentPhoneNumber(string phoneNumber)
        {
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = phoneNumber,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // POST: /Manage/RemoveImagePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveImagePassword()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                user.SerialHash = null;
                await UserManager.UpdateAsync(user);
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemoveImagePasswordSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    user.PasswordResetDate = DateTime.Now.AddDays(90);
                    await UserManager.UpdateAsync(user);
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                SendNotifications(user, "", NotificationType.ChangePassword);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        user.PasswordResetDate = DateTime.Now.AddDays(90);
                        await UserManager.UpdateAsync(user);
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

#region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error,
            AddImagePasswordSuccess,
            ChangeImagePasswordSuccess,
            RemoveImagePasswordSuccess,
            ChangeEmailSuccess,
            Clear2FASuccess,
            SentConfirmEmail,
            ImageLoginSuccess,
            ChangeNotificationSuccess
        }

#endregion
    }
}