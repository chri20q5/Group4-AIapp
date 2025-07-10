#!/bin/bash

echo "🚀 AutoJob Frontend - Azure App Service Deployment"

# Configuration
RESOURCE_GROUP="CloudIT-Group4"
APP_NAME="autojob-frontend"
LOCATION="West Europe"  # Close to your database
PLAN_NAME="autojob-app-service-plan"

# Check if logged into Azure
echo "📋 Checking Azure login status..."
if ! az account show > /dev/null 2>&1; then
    echo "❌ Not logged into Azure. Please run 'az login' first."
    exit 1
fi

echo "✅ Azure login confirmed"

# Get current subscription
SUBSCRIPTION=$(az account show --query id --output tsv)
echo "📊 Using subscription: $SUBSCRIPTION"

# Create or get resource group
echo "🏗️  Ensuring resource group exists..."
az group create \
    --name $RESOURCE_GROUP \
    --location "$LOCATION" \
    --output table

# Create App Service Plan (Free tier for testing)
echo "📦 Creating App Service Plan..."
az appservice plan create \
    --name $PLAN_NAME \
    --resource-group $RESOURCE_GROUP \
    --location "$LOCATION" \
    --sku F1 \
    --is-linux \
    --output table

# Create Web App
echo "🌐 Creating Web App..."
az webapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --plan $PLAN_NAME \
    --runtime "NODE:18-lts" \
    --output table

# Configure app settings
echo "⚙️  Configuring app settings..."
az webapp config appsettings set \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        WEBSITE_NODE_DEFAULT_VERSION="18.x" \
        DEFAULT_DOCUMENT="home.html" \
        HTTPS_ONLY="true" \
    --output table

# Deploy the static files
echo "📤 Deploying frontend files..."
cd "$(dirname "$0")"

# Create a temporary deployment package
echo "📦 Creating deployment package..."
mkdir -p ../tmp/deploy
cp *.html ../tmp/deploy/
cp *.css ../tmp/deploy/
cp *.js ../tmp/deploy/
cp web.config ../tmp/deploy/
cp package.json ../tmp/deploy/

# Deploy using ZIP deployment
cd ../tmp/deploy
zip -r ../autojob-frontend.zip .
cd ../../job-app-frontend

echo "🚀 Uploading to Azure..."
az webapp deployment source config-zip \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --src ../tmp/autojob-frontend.zip

# Clean up temporary files
rm -rf ../tmp

# Get the URL
URL=$(az webapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query defaultHostName --output tsv)

echo ""
echo "✅ Deployment completed!"
echo "🌐 Your AutoJob frontend is now available at: https://$URL"
echo "📊 App Service: $APP_NAME"
echo "📁 Resource Group: $RESOURCE_GROUP"
echo ""
echo "🔧 Next steps:"
echo "   1. Update your Azure Functions CORS settings to allow: https://$URL"
echo "   2. Update app.js with your Azure Functions URL"
echo "   3. Test the full workflow!"
echo ""
echo "💡 To update the site, just run this script again!"
