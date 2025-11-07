/**
 * Static Reference Data
 *
 * Cultural interests, languages, and proficiency levels for the profile feature.
 * Currently static, but designed with repository pattern for future API migration.
 */

export interface CulturalInterestOption {
  id: string;
  label: string;
  description?: string;
}

export interface LanguageOption {
  code: string; // ISO 639-1 or -2 code
  name: string;
  nativeName?: string;
}

export interface ProficiencyLevelOption {
  value: 'Basic' | 'Intermediate' | 'Advanced' | 'Native';
  label: string;
  description: string;
}

/**
 * Cultural Interests
 * Based on Sri Lankan culture, festivals, and traditions
 */
export const culturalInterests: CulturalInterestOption[] = [
  { id: 'art', label: 'Art & Painting', description: 'Visual arts, traditional and contemporary painting' },
  { id: 'music', label: 'Music', description: 'Traditional and contemporary music' },
  { id: 'dance', label: 'Dance', description: 'Traditional and folk dance forms' },
  { id: 'theatre', label: 'Theatre & Drama', description: 'Stage performances and theatrical arts' },
  { id: 'literature', label: 'Literature', description: 'Poetry, novels, and literary works' },
  { id: 'cuisine', label: 'Cuisine', description: 'Traditional and regional food culture' },
  { id: 'festivals', label: 'Festivals', description: 'Cultural and religious celebrations' },
  { id: 'handicrafts', label: 'Handicrafts', description: 'Traditional crafts and handmade items' },
  { id: 'textiles', label: 'Textiles & Fashion', description: 'Traditional clothing and textile arts' },
  { id: 'architecture', label: 'Architecture', description: 'Traditional and historical architecture' },
  { id: 'religion', label: 'Religion & Spirituality', description: 'Religious practices and spiritual traditions' },
  { id: 'martial-arts', label: 'Martial Arts', description: 'Traditional combat and self-defense arts' },
  { id: 'ayurveda', label: 'Ayurveda & Wellness', description: 'Traditional medicine and wellness practices' },
  { id: 'astrology', label: 'Astrology', description: 'Traditional astrological practices' },
  { id: 'heritage', label: 'Heritage Sites', description: 'Historical and archaeological sites' },
  { id: 'folk-traditions', label: 'Folk Traditions', description: 'Village customs and folk culture' },
  { id: 'rituals', label: 'Rituals & Ceremonies', description: 'Traditional ceremonies and rites' },
  { id: 'storytelling', label: 'Storytelling', description: 'Oral traditions and folklore' },
  { id: 'games', label: 'Traditional Games', description: 'Indigenous sports and games' },
  { id: 'agriculture', label: 'Agriculture', description: 'Traditional farming and cultivation practices' },
];

/**
 * Languages
 * Common languages spoken in Sri Lanka and globally
 */
export const languages: LanguageOption[] = [
  { code: 'si', name: 'Sinhala', nativeName: 'සිංහල' },
  { code: 'ta', name: 'Tamil', nativeName: 'தமிழ்' },
  { code: 'en', name: 'English' },
  { code: 'hi', name: 'Hindi', nativeName: 'हिन्दी' },
  { code: 'ar', name: 'Arabic', nativeName: 'العربية' },
  { code: 'zh', name: 'Chinese (Mandarin)', nativeName: '中文' },
  { code: 'zh-Hans', name: 'Chinese (Simplified)', nativeName: '简体中文' },
  { code: 'zh-Hant', name: 'Chinese (Traditional)', nativeName: '繁體中文' },
  { code: 'es', name: 'Spanish', nativeName: 'Español' },
  { code: 'fr', name: 'French', nativeName: 'Français' },
  { code: 'de', name: 'German', nativeName: 'Deutsch' },
  { code: 'it', name: 'Italian', nativeName: 'Italiano' },
  { code: 'pt', name: 'Portuguese', nativeName: 'Português' },
  { code: 'ru', name: 'Russian', nativeName: 'Русский' },
  { code: 'ja', name: 'Japanese', nativeName: '日本語' },
  { code: 'ko', name: 'Korean', nativeName: '한국어' },
  { code: 'bn', name: 'Bengali', nativeName: 'বাংলা' },
  { code: 'ur', name: 'Urdu', nativeName: 'اردو' },
  { code: 'te', name: 'Telugu', nativeName: 'తెలుగు' },
  { code: 'ml', name: 'Malayalam', nativeName: 'മലയാളം' },
];

/**
 * Proficiency Levels
 * Matches backend ProficiencyLevel enum
 */
export const proficiencyLevels: ProficiencyLevelOption[] = [
  {
    value: 'Basic',
    label: 'Basic',
    description: 'Can understand and use familiar everyday expressions',
  },
  {
    value: 'Intermediate',
    label: 'Intermediate',
    description: 'Can communicate in routine tasks requiring simple exchanges',
  },
  {
    value: 'Advanced',
    label: 'Advanced',
    description: 'Can produce clear, detailed text on a wide range of subjects',
  },
  {
    value: 'Native',
    label: 'Native',
    description: 'Native speaker or equivalent fluency',
  },
];
