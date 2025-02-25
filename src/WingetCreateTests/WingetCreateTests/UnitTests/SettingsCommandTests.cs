﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.WingetCreateUnitTests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.WingetCreateCLI;
    using Microsoft.WingetCreateCLI.Commands;
    using Microsoft.WingetCreateCLI.Logging;
    using Microsoft.WingetCreateCLI.Models.Settings;
    using Microsoft.WingetCreateCLI.Properties;
    using NUnit.Framework;

    /// <summary>
    /// Unit test class for the Settings Command.
    /// </summary>
    public class SettingsCommandTests
    {
        /// <summary>
        /// OneTimeSetup method for the settings command unit tests.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Logger.Initialize();
        }

        /// <summary>
        /// Setup method that removes the settings file and backup settings file prior to running tests.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            File.Delete(UserSettings.SettingsJsonPath);
            File.Delete(UserSettings.SettingsBackupJsonPath);
        }

        /// <summary>
        /// TearDown method that resets the winget-pkts repo owner and name to their default settings.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            UserSettings.WindowsPackageManagerRepositoryOwner = BaseCommand.DefaultWingetRepoOwner;
            UserSettings.WindowsPackageManagerRepositoryName = BaseCommand.DefaultWingetRepo;
        }

        /// <summary>
        /// Runs the settings command once which should generate a new settings file.
        /// </summary>
        [Test]
        public void VerifySettingsFileGenerated()
        {
            SettingsCommand command = new SettingsCommand();
            command.Execute();
            Assert.IsTrue(File.Exists(UserSettings.SettingsJsonPath), "Settings file was not created successfully.");
            Assert.IsTrue(UserSettings.ParseJsonFile(UserSettings.SettingsJsonPath, out SettingsManifest manifest).IsValid, "Generated settings file was not valid.");
        }

        /// <summary>
        /// Runs the settings command twice which should generate a second backup settings file.
        /// </summary>
        [Test]
        public void VerifyBackupSettingsFileGenerated()
        {
            SettingsCommand firstCommand = new SettingsCommand();
            firstCommand.Execute();
            SettingsCommand secondCommand = new SettingsCommand();
            secondCommand.Execute();
            Assert.IsTrue(File.Exists(UserSettings.SettingsBackupJsonPath), "Backup settings file was not created successfully.");
            Assert.IsTrue(UserSettings.ParseJsonFile(UserSettings.SettingsBackupJsonPath, out SettingsManifest manifest).IsValid, "Generated backup settings file was not valid.");
        }

        /// <summary>
        /// Runs the settings command and modifies the file which should display a "Manifest cannot be empty." error.
        /// </summary>
        [Test]
        public void VerifyEmptySettingsFile()
        {
            SettingsCommand command = new SettingsCommand();
            command.Execute();
            File.WriteAllText(UserSettings.SettingsJsonPath, string.Empty);
            (bool isValid, List<string> errors) = UserSettings.ParseJsonFile(UserSettings.SettingsJsonPath, out SettingsManifest manifest);
            Assert.IsFalse(isValid, "Empty settings manifest should fail validation");
            StringAssert.Contains("Manifest cannot be empty", errors.First(), "Error message should be caught.");
        }

        /// <summary>
        /// Compares the state of the telemetry.disabled field before and after to ensure the change is reflected accordingly.
        /// </summary>
        [Test]
        public void VerifySavingTelemetrySettings()
        {
            bool isDisabled = UserSettings.TelemetryDisabled;
            string testRepoOwner = "testRepoOwner";
            string testRepoName = "testRepoName";
            UserSettings.TelemetryDisabled = !isDisabled;
            UserSettings.WindowsPackageManagerRepositoryOwner = testRepoOwner;
            UserSettings.WindowsPackageManagerRepositoryName = testRepoName;
            UserSettings.ParseJsonFile(UserSettings.SettingsJsonPath, out SettingsManifest manifest);
            Assert.IsTrue(manifest.Telemetry.Disable == !isDisabled, "Changed Telemetry setting was not reflected in the settings file.");
            Assert.IsTrue(manifest.WindowsPackageManagerRepository.Owner == testRepoOwner, "Changed WindowsPackageManagerRepository.Owner setting was not reflected in the settings file.");
            Assert.IsTrue(manifest.WindowsPackageManagerRepository.Name == testRepoName, "Changed WindowsPackageManagerRepository.Name setting was not reflected in the settings file.");
        }

        /// <summary>
        /// Verifies that the RepositoryNotFound error message is shown if the repository settings are invalid.
        /// </summary>
        /// /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Test]
        public async Task VerifyInvalidGitHubRepoSettingShowsError()
        {
            string testRepoOwner = "testRepoOwner";
            string testRepoName = "testRepoName";
            UserSettings.WindowsPackageManagerRepositoryOwner = testRepoOwner;
            UserSettings.WindowsPackageManagerRepositoryName = testRepoName;

            StringWriter sw = new StringWriter();
            System.Console.SetOut(sw);
            UpdateCommand command = new UpdateCommand { Id = "testId" };
            await command.LoadGitHubClient();
            string result = sw.ToString();
            Assert.That(result, Does.Contain(string.Format(Resources.RepositoryNotFound_Error, testRepoOwner, testRepoName)), "Repository not found error should be shown");
        }
    }
}
