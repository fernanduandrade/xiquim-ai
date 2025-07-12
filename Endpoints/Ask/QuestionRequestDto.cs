namespace xiquim_api.Endpoints.Ask;

public sealed record QuestionRequestDto(List<Question> history, string question);

public sealed record Question(string role, string message);