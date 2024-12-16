from django.contrib.auth.decorators import login_required
from django.shortcuts import render
from django.contrib.auth.views import LoginView
from django.urls import reverse_lazy

class CustomAdminLoginView(LoginView):
    template_name = 'user/custom_admin_login.html'  # Your custom admin login template
    redirect_authenticated_user = True

    def get_success_url(self):
        # Redirect users to the custom dashboard on successful login
        return reverse_lazy('user/dashboard.html')


# @login_required
def dashboard_view(request):
    return render(request, 'user/dashboard.html')  # Replace with your dashboard template



from django.contrib.auth.decorators import login_required
from django.shortcuts import render, redirect
from .models import UserProfile
from .forms import UserProfileForm

@login_required
def user_dashboard(request):
    profile, created = UserProfile.objects.get_or_create(user=request.user)
    if request.method == "POST":
        form = UserProfileForm(request.POST, instance=profile)
        if form.is_valid():
            form.save()
            return redirect('dashboard')  # Redirect to the dashboard after saving
    else:
        form = UserProfileForm(instance=profile)
    return render(request, 'dashboard.html', {'form': form})
