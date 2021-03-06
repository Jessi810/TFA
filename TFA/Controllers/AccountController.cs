﻿using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TFA.Models;
using System.Net;
using Newtonsoft.Json.Linq;
using static TFA.Controllers.ManageController;

namespace TFA.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        private SecLogContext secLog = new SecLogContext();
        private ImageContext img = new ImageContext();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
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
        // GET: /Account/ChangeImagePassword
        public async Task<ActionResult> ChangeImagePassword()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }

            return View(img.Images.ToList());
        }

        //
        // POST: /Account/ChangeImagePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeImagePassword(AddImagePasswordViewModel model)
        {
            if (model.SerialHash == null)
            {
                //ViewBag.Message = "Please select an images to login";
                ModelState.AddModelError("", "Please select an images to login");
                return View(img.Images.ToList());
            }
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Invalid. Please try again.");
                return View(img.Images.ToList());
            }

            if (model.SerialHash.Length < 8)
            {
                ModelState.AddModelError("", "Please select at least 4 images");
                return View(img.Images.ToList());
            }

            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            // Encrypt serials
            var key = Helper.SerialImageEncryptor.GeneratePassword(16);
            var serialHash = Helper.SerialImageEncryptor.EncodePassword(model.SerialHash, key);

            user.SerialHash = serialHash;
            user.VCode = key;
            await UserManager.UpdateAsync(user);

            return RedirectToAction("Index", "Manage", new { Message = ManageMessageId.ChangeImagePasswordSuccess });
        }

        //
        // GET: /Account/AddImagePassword
        public async Task<ActionResult> AddImagePassword()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }

            return View(img.Images.ToList());
        }

        //
        // POST: /Account/AddImagePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddImagePassword(AddImagePasswordViewModel model)
        {
            if (model.SerialHash == null)
            {
                //ViewBag.Message = "Please select an images to login";
                ModelState.AddModelError("", "Please select an images to login");
                return View(img.Images.ToList());
            }

            if (!ModelState.IsValid)
            {
                return View(img.Images.ToList());
            }

            if (model.SerialHash.Length < 8)
            {
                ModelState.AddModelError("", "Please select at least 4 images");
                return View(img.Images.ToList());
            }

            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            // Encrypt serials
            var key = Helper.SerialImageEncryptor.GeneratePassword(16);
            var serialHash = Helper.SerialImageEncryptor.EncodePassword(model.SerialHash, key);

            user.SerialHash = serialHash;
            user.VCode = key;
            await UserManager.UpdateAsync(user);

            return RedirectToAction("Index", "Manage", new { Message = ManageMessageId.AddImagePasswordSuccess });
        }

        //
        // GET: /Account/ImageLogin
        [AllowAnonymous]
        public async Task<ActionResult> ImageLogin(string email)
        {
            if (String.IsNullOrEmpty(email))
            {
                return View("Error");
            }

            ApplicationUser user;
            try
            {
                user = await UserManager.FindByEmailAsync(email);
            }
            catch (Exception e)
            {
                return View("Error");
            }
            
            if(user.SerialHash == null)
            {
                return RedirectToAction("Index", "Manage");
            }

            TempData["UserEmail"] = email;

            return View(img.Images.ToList());
        }

        //
        // POST: /Account/ImageLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ImageLogin(ImageLoginViewModel model)
        {
            if (String.IsNullOrEmpty(model.Email))
            {
                return RedirectToAction("Login", "Account");
            }

            ApplicationUser user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (await UserManager.IsLockedOutAsync(user.Id))
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                SendNotifications(user, "Account Lockout", NotificationType.AccountLockout);

                return View("Lockout");
            }

            if (!ModelState.IsValid)
            {
                await UserManager.AccessFailedAsync(user.Id);
                ModelState.AddModelError("", "Please select the right images to login");
                TempData["UserEmail"] = model.Email;
                return View(img.Images.ToList());
            }

            // Check if user has selected and images
            if (model.ImageSerial == null)
            {
                await UserManager.AccessFailedAsync(user.Id);
                ModelState.AddModelError("", "Please select an images to continue.");
                TempData["UserEmail"] = model.Email;
                return View(img.Images.ToList());
                //return RedirectToAction("ImageLogin", new { Email = model.Email });
            }

            //if (model.ImageSerial == null)
            //{

            //    TempData["UserEmail"] = model.Email;
            //    //ViewBag.ImageLoginMessage = "Please select the right images to login";
            //    ModelState.AddModelError("", "Please select the right images to login");
            //    return View(img.Images.ToList());
            //}

            // Encode
            var hashCode = user.VCode;
            var encodeSerial = Helper.SerialImageEncryptor.EncodePassword(model.ImageSerial, hashCode);

            if (user.SerialHash.Equals(encodeSerial))
            {
                await UserManager.ResetAccessFailedCountAsync(user.Id);
                return RedirectToAction("Index", "Manage", new { Message = ManageMessageId.ImageLoginSuccess });
            }
            else
            {
                await UserManager.AccessFailedAsync(user.Id);

                TempData["UserEmail"] = model.Email;
                //ViewBag.ImageLoginMessage = "Invalid images selected. Please try again.";
                ModelState.AddModelError("", "Invalid images selected. Please try again.");
                return View(img.Images.ToList());
            }
        }

        //
        // GET: /Account/PasswordExpired
        [AllowAnonymous]
        public async Task<ActionResult> PasswordExpired(string email)
        {
            ApplicationUser user = await UserManager.FindByEmailAsync(email);
            string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(user.Id, "Reset Password", "Your password has expired. Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
            return View("PasswordExpiredConfirmation");
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool status = false;
            try
            {
                // Google reCaptcha validation
                var response = Request["g-recaptcha-response"];
                string secretKey = "6LeP5S0UAAAAAIRNJSYxs4maRDcPA107rwog9fD6";
                var client = new WebClient();
                var downloadStringResult = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secretKey, response));
                var obj = JObject.Parse(downloadStringResult);
                status = (bool)obj.SelectToken("success");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "No internet connection.");
                return View(model);
            }

            // Redisplay page if reCaptcha validation failed
            if (!status)
            {
                ViewBag.RecaptchaMessage = "Google reCaptcha validation failed. Please try again.";
                ModelState.AddModelError("", "Recaptcha validation failed. Please try again.");
                return View(model);
            }

            ApplicationUser user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }
            DateTime serverDate = DateTime.Now;
            var diff = user.PasswordResetDate.Subtract(serverDate);
            if (diff.Days < 0)
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

                return RedirectToAction("PasswordExpired", "Account", new { Email = user.Email });
            }


            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: true);
            if (UserManager.IsLockedOut(user.Id))
            {
                SendNotifications(user, "Account Lockout", NotificationType.AccountLockout);
            }
            switch (result)
            {
                case SignInStatus.Success:
                    user = await UserManager.FindByEmailAsync(model.Email);
                    if (!user.TwoFactorEnabled && user.ThreeFactorEnabled)
                    {
                        return RedirectToAction("ImageLogin", new { Email = model.Email });
                    }
                    if (String.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction("Index", "Manage");
                    }
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe, Email = model.Email });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
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
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe, string email)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe, Email = email });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: true);
            ApplicationUser user = await UserManager.FindByEmailAsync(model.Email);
            if (await UserManager.IsLockedOutAsync(user.Id))
            {
                SendNotifications(user, "Account Lockout", NotificationType.AccountLockout);
            }
            switch (result)
            {
                case SignInStatus.Success:
                    //ApplicationUser user = await UserManager.FindByEmailAsync(model.Email);
                    if (user.ThreeFactorEnabled)
                    {
                        return RedirectToAction("ImageLogin", new { Email = model.Email });
                    }

                    if (model.ReturnUrl == null)
                    {
                        return RedirectToAction("Index", "Manage");
                    }

                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordResetDate = DateTime.Now.AddDays(90)
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // POST: /Account/ConfirmEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendConfirmEmail()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

            string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

            return RedirectToAction("Index", "Manage", new { Message = ManageMessageId.SentConfirmEmail });
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                user.PasswordResetDate = DateTime.Now.AddDays(90);
                await UserManager.UpdateAsync(user);
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe, string email)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe, Email = email });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe, Email = model.Email });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
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

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}