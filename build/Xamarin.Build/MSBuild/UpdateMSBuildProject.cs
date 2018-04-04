// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class UpdateMSBuildProject : Task
    {
        public string Project { get; set; }

        public bool AsNewProject { get; set; }

        public ITaskItem [] Properties { get; set; }

        public override bool Execute ()
        {
            var projectCollection = new ProjectCollection ();
            var project = AsNewProject
                ? new Project (projectCollection, NewProjectFileOptions.None) { FullPath = Project }
                : projectCollection.LoadProject (Project);

            if (Properties != null) {
                foreach (var property in Properties) {
                    var projectProperty = project.SetProperty (property.ItemSpec, property.GetMetadata ("Value"));
                    projectProperty.Xml.Condition = $"'$({property.ItemSpec})' == ''";
                }
            }

            if (!File.Exists (project.FullPath)) {
                project.Save (Encoding.UTF8);
                return true;
            }

            project.Save (Path.GetTempFileName (), Encoding.UTF8);

            if (File.ReadAllText (project.FullPath) != File.ReadAllText (Project)) {
                File.Delete (Project);
                File.Move (project.FullPath, Project);
            } else {
                File.Delete (project.FullPath);
            }

            return true;
        }
    }
}