from django import forms
from .models import UserProfile

class UserProfileForm(forms.ModelForm):
    class Meta:
        model = UserProfile
        fields = [
            'company',
            'division',
            'unit_group',
            'office_location',
            'supervisor',
            'business_mobile',
            'otp_mobile',
            'telephone',
        ]

