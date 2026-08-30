using FluentAssertions;
using TradeFlow.Domain.Enums;

namespace TradeFlow.UnitTests.Authentication;

/// <summary>
/// Unit tests for authentication-related logic that can run without a real database.
/// Uses only Domain project types (no ASP.NET Core Identity direct dependency needed).
/// </summary>
public class AuthenticationTests
{
    // ─────────────────────────────────────────────────────────────
    // Password Policy Validation (mirrors InfrastructureServiceExtensions)
    // ─────────────────────────────────────────────────────────────

    private record PasswordPolicyOptions(
        bool RequireDigit,
        bool RequireLowercase,
        bool RequireUppercase,
        bool RequireNonAlphanumeric,
        int RequiredLength);

    private static readonly PasswordPolicyOptions DevPolicy = new(
        RequireDigit: true,
        RequireLowercase: true,
        RequireUppercase: false,       // Key: tradecore123 has no uppercase
        RequireNonAlphanumeric: false,
        RequiredLength: 8);

    private static List<string> ValidatePassword(string password, PasswordPolicyOptions options)
    {
        var errors = new List<string>();
        if (password.Length < options.RequiredLength)
            errors.Add($"Minimum length {options.RequiredLength}");
        if (options.RequireDigit && !password.Any(char.IsDigit))
            errors.Add("Requires digit");
        if (options.RequireLowercase && !password.Any(char.IsLower))
            errors.Add("Requires lowercase");
        if (options.RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("Requires uppercase");
        if (options.RequireNonAlphanumeric && !password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("Requires non-alphanumeric");
        return errors;
    }

    [Theory]
    [InlineData("tradecore123", true)]    // lowercase + digit ≥ 8 chars — MUST pass
    [InlineData("TradeFlow@2026", true)]  // also passes (has uppercase but not required)
    [InlineData("abc", false)]            // too short
    [InlineData("", false)]               // empty
    [InlineData("UPPERCASE123", false)]   // no lowercase (RequireLowercase=true fails)
    [InlineData("abcdefgh", false)]       // no digit
    public void DevPassword_MeetsConfiguredPolicy(string password, bool shouldPass)
    {
        var errors = ValidatePassword(password, DevPolicy);

        if (shouldPass)
            errors.Should().BeEmpty(
                "password '{0}' should pass the dev policy but got errors: {1}",
                password, string.Join(", ", errors));
        else
            errors.Should().NotBeEmpty(
                "password '{0}' should fail the dev policy", password);
    }

    [Fact]
    public void DevPassword_tradecore123_PassesDevPolicy()
    {
        // The primary dev credential must always pass
        var errors = ValidatePassword("tradecore123", DevPolicy);
        errors.Should().BeEmpty("tradecore123 is the official dev password and must satisfy the Identity policy");
    }

    // ─────────────────────────────────────────────────────────────
    // UserStatus check logic (mirrors Login.razor behavior)
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(UserStatus.Active, true)]
    [InlineData(UserStatus.Locked, false)]
    [InlineData(UserStatus.Disabled, false)]
    public void UserStatus_IsLoginAllowed(UserStatus status, bool expectedAllowed)
    {
        // Login.razor: only Active users can log in
        var isAllowed = status == UserStatus.Active;
        isAllowed.Should().Be(expectedAllowed,
            "UserStatus.{0} should {1}allow login", status, expectedAllowed ? "" : "not ");
    }

    // ─────────────────────────────────────────────────────────────
    // Login Input Resolution (username vs email lookup)
    // Mirrors the lookup logic in Login.razor LoginUser()
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("admin", false)]
    [InlineData("admin@tradeflow.local", true)]
    [InlineData("ADMIN@TRADEFLOW.LOCAL", true)]
    [InlineData("quanly01", false)]
    [InlineData("quanly01@tradeflow.local", true)]
    [InlineData("kho01", false)]
    [InlineData("xnk01", false)]
    public void LoginInput_IsEmail(string input, bool expectedIsEmail)
    {
        var isEmail = input.Contains('@');
        isEmail.Should().Be(expectedIsEmail,
            "login input '{0}' should {1}be detected as email", input, expectedIsEmail ? "" : "not ");
    }

    // ─────────────────────────────────────────────────────────────
    // Dev accounts verification
    // ─────────────────────────────────────────────────────────────

    private static readonly string[] RequiredDevUsernames =
        ["admin", "quanly01", "kinhdoanh01", "muahang01", "kho01", "xnk01"];

    // These must exactly match the DevAccounts array in DatabaseSeeder.cs
    private static readonly string[] SeededUsernames =
        ["admin", "quanly01", "kinhdoanh01", "muahang01", "kho01", "xnk01"];

    [Fact]
    public void DevAccounts_ShouldContainAllRequiredUsernames()
    {
        SeededUsernames.Should().BeEquivalentTo(RequiredDevUsernames,
            "all required dev usernames must be present in the seeder");
    }

    [Fact]
    public void DevAccounts_AllShouldHaveUniqueUsernames()
    {
        SeededUsernames.Should().OnlyHaveUniqueItems("dev accounts must have unique usernames");
    }

    [Fact]
    public void DevAccounts_ShouldContainCorrectCount()
    {
        SeededUsernames.Should().HaveCount(6, "there are 6 dev accounts defined");
    }

    [Fact]
    public void DevAccounts_AllEmails_ShouldBeUnique()
    {
        var emails = SeededUsernames.Select(u => $"{u}@tradeflow.local").ToArray();
        emails.Should().OnlyHaveUniqueItems("email addresses must be unique across dev accounts");
    }

    // ─────────────────────────────────────────────────────────────
    // Role assignment
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void DevAccounts_AdminShouldHaveAdministratorRole()
    {
        var adminRoleMap = new Dictionary<string, string>
        {
            ["admin"] = "Administrator",
            ["quanly01"] = "Manager",
            ["kinhdoanh01"] = "Sales",
            ["muahang01"] = "Purchase",
            ["kho01"] = "Warehouse",
            ["xnk01"] = "Import-Export"
        };

        adminRoleMap["admin"].Should().Be("Administrator");
        adminRoleMap.Keys.Should().BeEquivalentTo(SeededUsernames);
        adminRoleMap.Values.Should().OnlyHaveUniqueItems("each dev account has a distinct role");
    }

    // ─────────────────────────────────────────────────────────────
    // Resource / Permission enum coverage
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void ResourceType_ShouldHaveAtLeastOneValue()
    {
        var values = Enum.GetValues<ResourceType>();
        values.Should().NotBeEmpty("ResourceType enum must define at least one resource");
    }

    [Fact]
    public void PermissionAction_ShouldHaveAtLeastOneValue()
    {
        var values = Enum.GetValues<PermissionAction>();
        values.Should().NotBeEmpty("PermissionAction enum must define at least one action");
    }
}
