namespace DEM.Net.TestWinForm
{
    partial class FormTestLab
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.ctrlTestLab1 = new DEM.Net.TestWinForm.CtrlTestLab();
            this.SuspendLayout();
            // 
            // ctrlTestLab1
            // 
            this.ctrlTestLab1.Location = new System.Drawing.Point(12, 38);
            this.ctrlTestLab1.Name = "ctrlTestLab1";
            this.ctrlTestLab1.Size = new System.Drawing.Size(697, 372);
            this.ctrlTestLab1.TabIndex = 0;
            // 
            // FormTestLab
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(747, 422);
            this.Controls.Add(this.ctrlTestLab1);
            this.Name = "FormTestLab";
            this.Text = "Test Lab";
            this.ResumeLayout(false);

        }

        #endregion

        private CtrlTestLab ctrlTestLab1;
    }
}

