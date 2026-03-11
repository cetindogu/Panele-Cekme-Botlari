#!/bin/bash
# VM üzerinde Git pull-based deployment kurulumu

REPO_DIR="/opt/tronpanel-repo"
APP_DIR="/opt/tronpanel"

sudo mkdir -p $REPO_DIR
sudo mkdir -p $APP_DIR
sudo chown -R $USER:$USER $REPO_DIR
sudo chown -R $USER:$USER $APP_DIR

cd $REPO_DIR
if [ ! -d ".git" ]; then
    git init --bare
fi

# Post-receive hook
cat > $REPO_DIR/hooks/post-receive <<EOF
#!/bin/bash
GIT_WORK_TREE=$APP_DIR git checkout -f
cd $APP_DIR
# Build if needed (but we are building locally and pushing DLLs usually)
# If building on VM:
# dotnet publish -c Release -o /opt/tronpanel/publish
sudo systemctl restart tronpanel.service
EOF

chmod +x $REPO_DIR/hooks/post-receive

echo "Git repository created at $REPO_DIR"
echo "You can add it as a remote: git remote add gcp ssh://$USER@$VM_IP$REPO_DIR"
