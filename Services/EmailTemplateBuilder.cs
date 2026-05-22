using System.Net;

namespace HealthInsuranceManagement.Services
{
    public static class EmailTemplateBuilder
    {
        public static string BuildNotificationEmail(
            string subject,
            string body,
            string? recipientName,
            string? recipientRole,
            string eventType,
            int? relatedEntityId)
        {
            var safeSubject = WebUtility.HtmlEncode(subject);
            var greetingName = FriendlyName(recipientName);
            var roleLabel = string.IsNullOrWhiteSpace(recipientRole)
                ? "System user"
                : WebUtility.HtmlEncode(recipientRole);
            var eventLabel = EventLabel(eventType);
            var eventExplanation = EventExplanation(eventType);
            var referenceText = relatedEntityId.HasValue
                ? $"Reference #{relatedEntityId.Value}"
                : "No reference number";
            var updateIcon = BootstrapIcon("bell-fill", "Update type");
            var roleIcon = BootstrapIcon("person-badge", "Recipient role");
            var trackingIcon = BootstrapIcon("hash", "Tracking reference");
            var detailsIcon = BootstrapIcon("card-text", "Message details");

            return BuildShell(
                safeSubject,
                $"""
                <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;margin:0 0 18px;">
                    <tr>
                        <td style="padding:20px;background:#f4fbfa;border:1px solid #cfe7e3;border-radius:16px;">
                            <p style="margin:0 0 8px;color:#0f766e;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:.08em;">Workflow notification</p>
                            <p style="margin:0;color:#253342;font-size:16px;line-height:1.7;">
                                Hello <strong style="color:#0f172a;">{greetingName}</strong>, Health Insurance Management sent this update because a workflow item changed.
                                {eventExplanation}
                            </p>
                        </td>
                    </tr>
                </table>

                <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:separate;border-spacing:0;margin:0 0 18px;">
                    <tr>
                        <td width="33.33%" style="padding:0 8px 0 0;vertical-align:top;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;background:#ffffff;border:1px solid #d9ece8;border-radius:14px;">
                                <tr>
                                    <td style="padding:14px;">
                                        <p style="margin:0 0 8px;color:#0f766e;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:.06em;">{updateIcon} Update</p>
                                        <p style="margin:0;color:#0f172a;font-size:15px;font-weight:700;line-height:1.4;">{eventLabel}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td width="33.33%" style="padding:0 4px;vertical-align:top;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;background:#ffffff;border:1px solid #d9ece8;border-radius:14px;">
                                <tr>
                                    <td style="padding:14px;">
                                        <p style="margin:0 0 8px;color:#0f766e;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:.06em;">{roleIcon} Role</p>
                                        <p style="margin:0;color:#0f172a;font-size:15px;font-weight:700;line-height:1.4;">{roleLabel}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td width="33.33%" style="padding:0 0 0 8px;vertical-align:top;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;background:#ffffff;border:1px solid #d9ece8;border-radius:14px;">
                                <tr>
                                    <td style="padding:14px;">
                                        <p style="margin:0 0 8px;color:#0f766e;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:.06em;">{trackingIcon} Tracking</p>
                                        <p style="margin:0;color:#0f172a;font-size:15px;font-weight:700;line-height:1.4;">{referenceText}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <div style="margin:0;padding:20px;background:#ffffff;border:1px solid #e2e8f0;border-radius:16px;color:#253342;font-size:15px;line-height:1.7;box-shadow:0 8px 20px rgba(15,23,42,.05);">
                    <p style="margin:0 0 12px;color:#0f172a;font-size:14px;font-weight:800;text-transform:uppercase;letter-spacing:.06em;">{detailsIcon} Message Details</p>
                    {NormalizeBody(body)}
                </div>
                """);
        }

        public static string BuildPasswordResetEmail(string resetUrl, string accountType)
        {
            var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
            var safeAccountType = WebUtility.HtmlEncode(accountType);

            return BuildShell(
                "Password reset request",
                $"""
                <p style="margin:0 0 16px;color:#475569;font-size:15px;line-height:1.7;">
                    We received a request to reset the password for your Health Insurance Management {safeAccountType} account.
                    For your security, this link can be used one time only and will expire after 30 minutes.
                </p>

                <div style="margin:18px 0;padding:18px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:14px;">
                    <p style="margin:0 0 10px;color:#0f172a;font-size:15px;font-weight:700;">Before you continue</p>
                    <p style="margin:0;color:#475569;font-size:14px;line-height:1.7;">
                        Choose a strong password with at least 8 characters, including a letter, a number, and a symbol.
                        Do not reuse an old password or share this reset link with anyone.
                    </p>
                </div>

                <p style="margin:22px 0;text-align:center;">
                    <a href="{safeResetUrl}" style="display:inline-block;padding:13px 22px;background:#006a6a;color:#ffffff;text-decoration:none;border-radius:10px;font-weight:700;font-size:15px;">Reset Password</a>
                </p>

                <div style="margin:18px 0;padding:16px;background:#fff8e6;border:1px solid #f7d98b;border-radius:14px;">
                    <p style="margin:0 0 8px;color:#854d0e;font-size:14px;font-weight:700;">Did not request this?</p>
                    <p style="margin:0;color:#713f12;font-size:14px;line-height:1.7;">
                        You can ignore this email. Your current password will remain unchanged unless this secure link is used before it expires.
                    </p>
                </div>

                <p style="margin:18px 0 0;color:#64748b;font-size:13px;line-height:1.7;">
                    If the button does not open, copy this link into your browser:<br>
                    <a href="{safeResetUrl}" style="color:#006a6a;word-break:break-all;">{safeResetUrl}</a>
                </p>
                """);
        }

        public static string BuildSupportReplyEmail(string name, string supportName, string replyText)
        {
            var safeName = WebUtility.HtmlEncode(name);
            var safeSupportName = WebUtility.HtmlEncode(supportName);
            var safeReply = WebUtility.HtmlEncode(replyText).Replace("\n", "<br>");

            return BuildShell(
                "Reply from Health Insurance Support",
                $"""
                <p style="margin:0 0 16px;color:#475569;font-size:15px;line-height:1.7;">
                    Hello {safeName}, our support team has reviewed your message and sent the following reply.
                    Please read it carefully and keep this email for your records.
                </p>

                <div style="margin:18px 0;padding:18px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:14px;color:#253342;font-size:15px;line-height:1.7;">
                    <p style="margin:0 0 10px;color:#0f172a;font-size:14px;font-weight:700;text-transform:uppercase;letter-spacing:.04em;">Support Reply</p>
                    <p style="margin:0;color:#253342;">{safeReply}</p>
                </div>

                <div style="margin:18px 0;padding:16px;background:#eef8f6;border:1px solid #d9ece8;border-radius:14px;">
                    <p style="margin:0;color:#0f766e;font-size:14px;line-height:1.7;">
                        If you still need help, reply through the contact support flow with the latest details so the team can continue from this conversation.
                    </p>
                </div>

                <p style="margin:18px 0 0;color:#475569;font-size:14px;line-height:1.7;">
                    Regards,<br>
                    <strong style="color:#0f172a;">{safeSupportName}</strong><br>
                    Health Insurance Support
                </p>
                """);
        }

        private static string BuildShell(string title, string content)
        {
            const string logoMarkup = """<div style="width:48px;height:48px;border-radius:14px;background:#ffffff;border:1px solid rgba(255,255,255,.75);overflow:hidden;display:flex;align-items:center;justify-content:center;"> <img src="https://rutaab3.github.io/docs/favicon.png" alt="Health Insurance Management" style="display:block;width:48px;height:48px;object-fit:contain;" /></div>""";

            return $"""
                <!doctype html>
                <html>
                <body style="margin:0;padding:0;background:#eef6f4;font-family:Arial,Helvetica,sans-serif;color:#253342;">
                    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;background:#eef6f4;margin:0;padding:0;">
                        <tr>
                            <td align="center" style="padding:32px 14px;">
                                <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width:700px;border-collapse:collapse;background:#ffffff;border-radius:22px;overflow:hidden;border:1px solid #cfe7e3;box-shadow:0 16px 36px rgba(15,118,110,.14);">
                                    <tr>
                                        <td style="padding:26px 28px 24px;background:#006a6a;">
                                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse:collapse;">
                                                <tr>
                                                    <td style="width:58px;vertical-align:middle;">
                                                        {logoMarkup}
                                                    </td>
                                                    <td style="vertical-align:middle;padding-left:14px;">
                                                        <p style="margin:0;color:#d7fffb;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:.1em;">Health Insurance Management</p>
                                                        <h1 style="margin:7px 0 0;color:#ffffff;font-size:25px;line-height:1.25;font-weight:800;">{title}</h1>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:26px 28px 28px;background:#fbfefd;">
                                            {content}
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:18px 28px;background:#f8fafc;border-top:1px solid #e2e8f0;">
                                            <p style="margin:0;color:#64748b;font-size:12px;line-height:1.6;">
                                                This is an automated message from Health Insurance Management. Please verify sensitive workflow changes from inside the application before taking action.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                """;
        }

        private static string BootstrapIcon(string iconName, string altText)
        {
            var safeIconName = WebUtility.HtmlEncode(iconName);
            var safeAltText = WebUtility.HtmlEncode(altText);

            return $"""<img src="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/icons/{safeIconName}.svg" width="15" height="15" alt="{safeAltText}" style="display:inline-block;width:15px;height:15px;vertical-align:-2px;margin-right:6px;" />""";
        }

        private static string FriendlyName(string? name)
        {
            return string.IsNullOrWhiteSpace(name)
                ? "there"
                : WebUtility.HtmlEncode(name.Trim());
        }

        private static string NormalizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "<p style=\"margin:0;color:#475569;font-size:14px;line-height:1.7;\">No extra message details were provided.</p>";

            return body
                .Replace("<p>", "<p style=\"margin:0 0 10px;color:#475569;font-size:14px;line-height:1.7;\">")
                .Replace("<p><strong>", "<p style=\"margin:0 0 10px;color:#475569;font-size:14px;line-height:1.7;\"><strong style=\"color:#0f172a;\">")
                .Replace("<strong>", "<strong style=\"color:#0f172a;\">");
        }

        private static string EventLabel(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType))
                return "Workflow update";

            var chars = new List<char>();
            foreach (var ch in eventType.Trim())
            {
                if (chars.Count > 0 && char.IsUpper(ch) && chars[^1] != ' ')
                    chars.Add(' ');
                chars.Add(ch);
            }

            return WebUtility.HtmlEncode(new string(chars.ToArray()));
        }

        private static string EventExplanation(string eventType)
        {
            return eventType switch
            {
                "CompanyAdded" => "A new insurance company has been added to the resource list, so administrators should confirm the contact details before policies are attached to it.",
                "CompanyUpdated" => "An existing insurance company profile was changed, which may affect policy contact information and future staff reference.",
                "CompanyDeactivated" => "A company was deactivated and should no longer be treated as an active provider for new policy activity.",
                "CompanyActivated" => "A company was reactivated and can be used again in policy and administrative workflows.",
                "PolicyAdded" => "A new insurance policy is available in the system, so staff may begin reviewing it for employee requests or assignments.",
                "PolicyUpdated" => "An insurance policy was updated, so staff should recheck the latest premium, company, and policy information before using it.",
                "PolicyDeactivated" => "A policy was deactivated and should not be used for new assignments or requests until it is activated again.",
                "PolicyActivated" => "A policy is active again and available for staff workflows and employee visibility.",
                "EmployeeRegistered" => "A new staff account has been created, and the recipient should review the role and account information before using the dashboard.",
                "EmployeeUpdated" => "A staff profile was changed by an administrator, so the recipient should confirm the updated account details.",
                "EmployeeDeactivated" => "A staff account has been deactivated, which limits dashboard access until an administrator activates it again.",
                "EmployeeActivated" => "A staff account has been activated and can now access the role dashboard again.",
                "PolicyAssigned" => "A policy was assigned to an employee, so coverage dates and provider details should be reviewed carefully.",
                "PolicyRequestSubmitted" => "An employee submitted a policy request, and the workflow now needs review from the correct approval role.",
                "ManagerDecision" => "A manager has reviewed a policy request and recorded a decision that affects the request status.",
                "ForwardedToFinance" => "A manager-approved bill was forwarded to finance, so payment processing can continue.",
                "PaymentCredited" => "Finance has credited payment for a policy request, and the request is now ready for final review or closure.",
                "RequestClosed" => "A policy request was closed after payment, completing the active billing workflow for this request.",
                "AdminRequestDecision" => "An administrator processed a policy request, and the final status should be checked against the employee record.",
                "ContactQueryReply" => "Support has replied to a visitor query, and the conversation history should stay available for follow-up.",
                _ => "The details below explain what changed and who it affects."
            };
        }

    }
}
